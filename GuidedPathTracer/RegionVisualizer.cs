using System;
using System.Numerics;
using OpenPGL.NET;
using SeeSharp.Geometry;
using SeeSharp.Integrators;
using SimpleImageIO;

namespace GuidedPathTracer {
    public class RegionVisualizer : DebugVisualizer {
        public static RgbColor HsvToRgb(float hue, float saturation, float value) {
            float f(float n) {
                float k = (n + hue / 60) % 6;
                return value - value * saturation * Math.Clamp(Math.Min(k, 4 - k), 0, 1);
            }
            return new(f(5), f(3), f(1));
        }

        public Field GuidingField { get; set; }

        public override RgbColor ComputeColor(SurfacePoint hit, Vector3 from) {
            var value = base.ComputeColor(hit, from).Average;

            // Look up the containing region of the guiding field
            SurfaceSamplingDistribution distribution = new(GuidingField);
            distribution.Init(new(0.5f, 0.5f, 0), 0.5f);
            var region = distribution.Region;

            // Assign a color to this region based on its hash code
            int hash = region.GetHashCode();
            System.Random colorRng = new(hash);
            float hue = (float)colorRng.Next(360);
            float saturation = (float)colorRng.NextDouble() * 0.8f + 0.2f;

            return HsvToRgb(hue, saturation, value);
        }
    }
}