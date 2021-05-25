using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using OpenPGL.NET;
using SeeSharp.Geometry;
using SimpleImageIO;
using TinyEmbree;

namespace GuidedPathTracer {
    public class GuidedPathTracer : PathLenLoggingPathTracer {
        ThreadLocal<PathSegmentStorage> pathStorage = new();
        SampleStorage sampleStorage;
        ThreadLocal<SurfaceSamplingDistribution> distributionBuffer;

        public SpatialSettings SpatialSettings = new KdTreeSettings() { KnnLookup = true };

        public Field GuidingField;
        public bool GuidingEnabled { get; private set; }

        protected override void OnPrepareRender() {
            GuidingField = new(new(){
                SpatialSettings = SpatialSettings
            });
            GuidingField.SceneBounds = new() {
                Lower = scene.Bounds.Min - scene.Bounds.Diagonal * 0.01f,
                Upper = scene.Bounds.Max + scene.Bounds.Diagonal * 0.01f
            };

            sampleStorage = new();
            int numPixels = scene.FrameBuffer.Width * scene.FrameBuffer.Height;
            sampleStorage.Reserve((uint)(MaxDepth * numPixels), 0);

            distributionBuffer = new(() => new(GuidingField));

            GuidingEnabled = false;

            base.OnPrepareRender();
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
            float roughness = hit.Material.GetRoughness(hit);
            if (roughness < 0.1f) return 0;
            if (hit.Material.IsTransmissive(hit)) return 0;
            return 0.5f;
        }

        protected SurfaceSamplingDistribution GetDistribution(Vector3 outDir, SurfacePoint hit, PathState state) {
            SurfaceSamplingDistribution distribution = null;

            if (GuidingEnabled) {
                distribution = distributionBuffer.Value;
                distribution.Init(hit.Position, state.Rng.NextFloat(), useParallaxCompensation: true);
                distribution.ApplyCosineProduct(hit.ShadingNormal);

                Debug.Assert(distribution.IsValid);
            }
            return distribution;
        }

        protected override (Ray, float, RgbColor) SampleDirection(Ray ray, SurfacePoint hit, PathState state) {
            Vector3 outDir = Vector3.Normalize(-ray.Direction);
            float selectGuideProb = GuidingEnabled ? ComputeGuidingSelectProbability(outDir, hit, state) : 0;

            SurfaceSamplingDistribution distribution = null;
            if (selectGuideProb > 0) {
                distribution = GetDistribution(outDir, hit, state);
                Debug.Assert(distribution != null);
            }

            Ray nextRay;
            float guidePdf = 0, bsdfPdf;
            RgbColor contrib;
            if (state.Rng.NextFloat() < selectGuideProb) { // sample guided
                var sampledDir = distribution.Sample(state.Rng.NextFloat2D());
                guidePdf = distribution.PDF(sampledDir);
                guidePdf *= selectGuideProb;

                bsdfPdf = hit.Material.Pdf(hit, outDir, sampledDir, false).Item1;
                bsdfPdf *= (1 - selectGuideProb);

                contrib = hit.Material.EvaluateWithCosine(hit, outDir, sampledDir, false);
                contrib /= guidePdf + bsdfPdf;

                nextRay = Raytracer.SpawnRay(hit, sampledDir);
            } else { // Sample the BSDF (default)
                (nextRay, bsdfPdf, contrib) = base.SampleDirection(ray, hit, state);
                bsdfPdf *= (1 - selectGuideProb);

                if (bsdfPdf == 0) { // prevent NaNs / Infs
                    return (new(), 0, RgbColor.Black);
                }

                if (GuidingEnabled && selectGuideProb > 0) {
                    Debug.Assert(MathF.Abs(nextRay.Direction.LengthSquared() - 1) < 0.001f);
                    guidePdf = distribution.PDF(nextRay.Direction) * selectGuideProb;

                    // Apply balance heuristic
                    contrib *= bsdfPdf / (1 - selectGuideProb) / (guidePdf + bsdfPdf);
                }
            }

            distribution?.Clear();

            float pdf = guidePdf + bsdfPdf;
            if (pdf == 0) { // prevent NaNs / Infs
                return (new(), 0, RgbColor.Black);
            }

            // Update the incident direction and PDF in the current path segment
            var segment = pathStorage.Value.GetSegment(state.Depth - 1);
            var inDir = Vector3.Normalize(nextRay.Direction);
            segment.DirectionIn = inDir;
            segment.PDFDirectionIn = pdf;
            segment.ScatteringWeight = contrib;

            // Material data
            segment.Roughness = hit.Material.GetRoughness(hit);
            if (Vector3.Dot(inDir, -ray.Direction) < 0) {
                float ior = hit.Material.GetIndexOfRefractionRatio(hit);
                if (Vector3.Dot(inDir, hit.ShadingNormal) < 0) {
                    ior = 1 / ior;
                }
                segment.Eta = ior;
            } else {
                segment.Eta = 1;
            }

            return (nextRay, pdf, contrib);
        }

        protected override float DirectionPdf(SurfacePoint hit, Vector3 outDir, Vector3 sampledDir,
                                              PathState state) {
            float selectGuideProb = GuidingEnabled ? ComputeGuidingSelectProbability(outDir, hit, state) : 0;

            if (!GuidingEnabled || selectGuideProb <= 0)
                return base.DirectionPdf(hit, outDir, sampledDir, state);

            SurfaceSamplingDistribution distribution = GetDistribution(outDir, hit, state);

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
            segment.ScatteredContribution = contrib;
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
            segment.DirectContribution = emission;
        }

        protected override void OnFinishedPath(RadianceEstimate estimate, PathState state) {
            base.OnFinishedPath(estimate, state);

            if (!pathStorage.IsValueCreated) {
                return; // The path never hit anything.
            }

            // Generate the samples and add them to the global cache
            // TODO provide sampler (with more efficient wrapper)
            SamplerWrapper sampler = new(null, null);
            uint num = pathStorage.Value.PrepareSamples(
                sampler: sampler,
                splatSamples: false,
                useNEEMiWeights: false,
                guideDirectLight: false);
            sampleStorage.AddSamples(pathStorage.Value.SamplesRawPointer, num);
        }
    }
}