using System.Numerics;
using SeeSharp.Geometry;
using SeeSharp.Sampling;
using SimpleImageIO;
using TinyEmbree;

namespace GuidedPathTracer {
    public class GuidedPathTracer : SeeSharp.Integrators.PathTracer {
        protected override void OnPreIteration(uint iterIdx) {
            base.OnPreIteration(iterIdx);

            // TODO initialize / update the guiding distribution
        }
        protected override (Ray, float, RgbColor) SampleDirection(Ray ray, SurfacePoint hit, RNG rng) {
            return base.SampleDirection(ray, hit, rng);

            // TODO sample a direction from the guiding distribution (or BSDF)
        }

        protected override float DirectionPdf(SurfacePoint hit, Vector3 outDir, Vector3 sampledDir) {
            return base.DirectionPdf(hit, outDir, sampledDir);

            // TODO evaluate the pdf of guiding and/or BSDF sampling
        }

        protected override void RegisterRadianceEstimate(SurfacePoint hit, Vector3 outDir, Vector3 inDir,
                                                         RgbColor directIllum, RgbColor indirectIllum,
                                                         Vector2 pixel, RgbColor throughput, float pdfInDir,
                                                         float pdfNextEvent) {
            // TODO update the guiding distribution / log the sample contribution
        }
    }
}