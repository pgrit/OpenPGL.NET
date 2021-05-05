using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using OpenPGL.NET;
using SeeSharp.Geometry;
using SeeSharp.Sampling;
using SimpleImageIO;
using TinyEmbree;

namespace GuidedPathTracer {
    public class GuidedPathTracer : PathTracer {
        ThreadLocal<PathSegmentStorage> pathStorage = new();
        SampleStorage sampleStorage;
        ThreadLocal<SurfaceSamplingDistribution> distributionBuffer = new(() => new());

        public Field GuidingField;
        public bool GuidingEnabled { get; private set; }

        protected override void OnPrepareRender() {
            GuidingField = new(new(){
                SpatialSettings = new KdTreeSettings() { KnnLookup = false }
            });
            GuidingField.SceneBounds = new() {
                Lower = scene.Bounds.Min,
                Upper = scene.Bounds.Max
            };

            sampleStorage = new();
            int numPixels = scene.FrameBuffer.Width * scene.FrameBuffer.Height;
            sampleStorage.Reserve((uint)(MaxDepth * numPixels), 0);

            GuidingEnabled = false;
        }

        protected override void OnPostIteration(uint iterIdx) {
            GuidingField.Update(sampleStorage, 1);
            sampleStorage.Clear();

            GuidingEnabled = true;
        }

        protected override void OnStartPath(PathState state) {
            // Reserve memory for the path segments in our thread-local storage
            if (!pathStorage.IsValueCreated)
                pathStorage.Value = new();

            pathStorage.Value.Reserve((uint)MaxDepth + 1);
            pathStorage.Value.Clear();
        }

        protected override void OnHit(Ray ray, Hit hit, PathState state) {
            // Prepare the next path segment: set all the info we already have
            var segment = pathStorage.Value.NextSegment();

            // Geometry
            segment.Position = hit.Position;
            segment.DirectionOut = -Vector3.Normalize(ray.Direction);
            segment.Normal = hit.ShadingNormal;
        }

        protected virtual float ComputeGuidingSelectProbability(Vector3 outDir, SurfacePoint hit, PathState state) {
            return 0.5f;
        }

        protected SurfaceSamplingDistribution GetDistribution(Vector3 outDir, SurfacePoint hit, PathState state) {
            SurfaceSamplingDistribution distribution = null;
            if (GuidingEnabled) {
                SamplerWrapper sampler = new(state.Rng.NextFloat, state.Rng.NextFloat2D);
                Region region = GuidingField.GetSurfaceRegion(hit.Position, sampler);
                Debug.Assert(region.IsValid);

                distribution = distributionBuffer.Value;
                distribution.Init(region, hit.Position, useParallaxCompensation: true);
                distribution.ApplyCosineProduct(hit.ShadingNormal);

                Debug.Assert(distribution.IsValid);
            }
            return distribution;
        }

        protected override (Ray, float, RgbColor) SampleDirection(Ray ray, SurfacePoint hit, PathState state) {
            Vector3 outDir = Vector3.Normalize(-ray.Direction);
            SurfaceSamplingDistribution distribution = GetDistribution(outDir, hit, state);

            float selectGuideProb = GuidingEnabled ? ComputeGuidingSelectProbability(outDir, hit, state) : 0;
            Ray nextRay;
            float guidePdf = 0, bsdfPdf;
            RgbColor contrib;
            if (distribution != null && state.Rng.NextFloat() < selectGuideProb) { // sample guided
                var sampledDir = distribution.Sample(state.Rng.NextFloat2D());
                guidePdf = distribution.PDF(sampledDir);
                guidePdf *= selectGuideProb;

                bsdfPdf = hit.Material.Pdf(hit, outDir, sampledDir, false).Item1;
                bsdfPdf *= (1 - selectGuideProb);

                contrib = hit.Material.EvaluateWithCosine(hit, outDir, sampledDir, false);
                contrib /= guidePdf + bsdfPdf;

                nextRay = scene.Raytracer.SpawnRay(hit, sampledDir);
            } else { // Sample the BSDF (default)
                (nextRay, bsdfPdf, contrib) = base.SampleDirection(ray, hit, state);
                bsdfPdf *= (1 - selectGuideProb);

                if (GuidingEnabled && selectGuideProb > 0) {
                    Debug.Assert(MathF.Abs(nextRay.Direction.LengthSquared() - 1) < 0.001f);
                    guidePdf = distribution.PDF(nextRay.Direction) * selectGuideProb;

                    // Apply balance heuristic
                    contrib *= bsdfPdf / (1 - selectGuideProb) / (guidePdf + bsdfPdf);
                }
            }

            distribution?.Clear();

            // Update the incident direction and PDF in the current path segment
            float pdf = guidePdf + bsdfPdf;
            var segment = pathStorage.Value.GetSegment(state.Depth - 1);
            var inDir = Vector3.Normalize(nextRay.Direction);
            segment.DirectionIn = inDir;
            segment.PDFDirectionIn = pdf;
            segment.ScatteringWeight = new(contrib.R, contrib.G, contrib.B);

            // Material data
            segment.Roughness = hit.Material.GetRoughness(hit);
            segment.Eta = hit.Material.GetIndexOfRefractionRatio(hit, outDir, inDir);

            return (nextRay, pdf, contrib);
        }

        protected override float DirectionPdf(SurfacePoint hit, Vector3 outDir, Vector3 sampledDir,
                                              PathState state) {
            SurfaceSamplingDistribution distribution = GetDistribution(outDir, hit, state);
            float selectGuideProb = GuidingEnabled ? ComputeGuidingSelectProbability(outDir, hit, state) : 0;

            if (!GuidingEnabled || selectGuideProb <= 0)
                return base.DirectionPdf(hit, outDir, sampledDir, state);

            float bsdfPdf = base.DirectionPdf(hit, outDir, sampledDir, state) * (1 - selectGuideProb);
            float guidePdf = distribution.PDF(Vector3.Normalize(sampledDir)) * selectGuideProb;
            distribution?.Clear();
            return bsdfPdf + guidePdf;
        }

        protected override void OnNextEventResult(Ray ray, SurfacePoint point, PathState state,
                                                  float misWeight, RgbColor estimate) {
            var segment = pathStorage.Value.GetSegment(state.Depth - 1);

            var contrib = misWeight * estimate;
            // TODO this assumes that we have a single shadow ray. Should be += instead, which requires
            //      that the PGL API supports a getter for this value
            // TODO can use implicit cast from RgbColor -> Vector3 with latest SimpleImageIO version
            segment.ScatteredContribution = new(contrib.R, contrib.G, contrib.B);
        }

        protected override void OnHitLightResult(Ray ray, PathState state, float misWeight, RgbColor emission,
                                                 bool isBackground) {
            if (isBackground) {
                // We need to create the path segment first as there was no actual intersection
                var newSegment = pathStorage.Value.NextSegment();
                newSegment.DirectionOut = -Vector3.Normalize(ray.Direction);

                // We move the point far away enough for parallax compensation to no longer make a difference
                newSegment.Position = ray.Origin + ray.Direction * scene.Radius * 1337;
            }

            var segment = pathStorage.Value.GetSegment(state.Depth - 1);

            segment.MiWeight = misWeight;
            segment.DirectContribution = new(emission.R, emission.G, emission.B);
        }

        protected override void OnFinishedPath(Vector2 pixel, RNG rng, RadianceEstimate estimate) {
            if (!pathStorage.IsValueCreated) {
                return; // The path never hit anything.
            }

            // Generate the samples and add them to the global cache
            SamplerWrapper sampler = new(rng.NextFloat, rng.NextFloat2D);
            uint num = pathStorage.Value.PrepareSamples(
                sampler: sampler,
                splatSamples: false,
                useNEEMiWeights: false,
                guideDirectLight: false);

            sampleStorage.AddSamples(pathStorage.Value.Samples.ToArray());
        }
    }
}