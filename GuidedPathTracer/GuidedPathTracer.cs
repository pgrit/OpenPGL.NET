using System.Numerics;
using System.Threading;
using System.Diagnostics;
using OpenPGL.NET;
using SeeSharp.Geometry;
using SeeSharp.Sampling;
using SimpleImageIO;
using TinyEmbree;

namespace GuidedPathTracer {
    public class GuidedPathTracer : PathTracer {
        ThreadLocal<PathSegmentStorage> pathStorage = new();
        Field guidingField;
        SampleStorage sampleStorage;

        void InitGuiding() {
            guidingField = new();
            guidingField.SceneBounds = new() {
                Lower = scene.Bounds.Min,
                Upper = scene.Bounds.Min
            };

            sampleStorage = new();
            int numPixels = scene.FrameBuffer.Width * scene.FrameBuffer.Height;
            sampleStorage.Reserve((uint)(MaxDepth * numPixels), 0);
        }

        void UpdateGuiding() {
            guidingField.Update(sampleStorage, 1);
            sampleStorage.Clear();
        }

        protected override void OnPreIteration(uint iterIdx) {
            base.OnPreIteration(iterIdx);

            if (iterIdx == 0) {
                InitGuiding();
            } else {
                UpdateGuiding();
            }
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

            // Material data
            segment.Roughness = ((SurfacePoint)hit).Material.GetRoughness(hit);
            // segment.Eta = exteriorIOR / interiorIOR
        }

        protected override (Ray, float, RgbColor) SampleDirection(Ray ray, SurfacePoint hit, PathState state) {
            var (nextRay, pdf, contrib) = base.SampleDirection(ray, hit, state);

            // TODO sample a direction from the guiding distribution (or BSDF)

            // Update the incident direction and PDF in the current path segment
            var segment = pathStorage.Value.GetSegment(state.Depth - 1);
            segment.DirectionIn = Vector3.Normalize(nextRay.Direction);
            segment.PDFDirectionIn = pdf;

            return (nextRay, pdf, contrib);
        }

        protected override float DirectionPdf(SurfacePoint hit, Vector3 outDir, Vector3 sampledDir) {
            return base.DirectionPdf(hit, outDir, sampledDir);

            // TODO evaluate the pdf of guiding and/or BSDF sampling
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
                useNEEMiWeights: true,
                guideDirectLight: true);

            sampleStorage.AddSamples(pathStorage.Value.Samples.ToArray());
        }
    }
}