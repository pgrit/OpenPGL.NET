using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using OpenPGL.NET;
using SeeSharp.Geometry;
using SeeSharp.Images;
using SeeSharp.Shading.Materials;
using SimpleImageIO;
using TinyEmbree;

namespace GuidedPathTracer;

class SingleIterationLayer : RgbLayer {
    public override void OnStartIteration(int curIteration) => this.curIteration = 1;
}

public class GuidedPathTracer : PathLenLoggingPathTracer {
    ThreadLocal<PathSegmentStorage> pathStorage = new();
    SampleStorage sampleStorage;
    ThreadLocal<SurfaceSamplingDistribution> distributionBuffer;

    public SpatialSettings SpatialSettings = new KdTreeSettings() { KnnLookup = true };

    public Field GuidingField;
    public bool GuidingEnabled { get; private set; }

    /// <summary>
    /// If set to true, each iteration will be rendered as an individual layer in the .exr called
    /// "iter0001" etc. Also, the guiding caches of each iteration will be visualized in false color
    /// images in layers called "caches0001", "caches0002", ...
    /// </summary>
    public bool WriteIterationsAsLayers { get; set; }

    List<SingleIterationLayer> iterationRenderings = new();
    List<SingleIterationLayer> iterationCacheVisualizations = new();

    public override void RegisterSample(Pixel pixel, RgbColor weight, float misWeight, uint depth,
                                        bool isNextEvent) {
        base.RegisterSample(pixel, weight, misWeight, depth, isNextEvent);

        if (WriteIterationsAsLayers) {
            var render = iterationRenderings[scene.FrameBuffer.CurIteration - 1];
            render.Splat(pixel, weight * misWeight);
        }
    }

    protected override void OnPrepareRender() {
        GuidingField = new(new FieldSettings() {
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

        // Add additional frame buffer layers
        if (WriteIterationsAsLayers) {
            for (int i = 0; i < TotalSpp; ++i) {
                iterationRenderings.Add(new());
                iterationCacheVisualizations.Add(new());
                scene.FrameBuffer.AddLayer($"iter{i:0000}", iterationRenderings[^1]);
                scene.FrameBuffer.AddLayer($"caches{i:0000}", iterationCacheVisualizations[^1]);
            }
        }

        base.OnPrepareRender();
    }

    protected override void OnPostIteration(uint iterIdx) {
        GuidingField.Update(sampleStorage, 1);
        sampleStorage.Clear();
        GuidingEnabled = true;
    }

    protected override void OnStartPath(ref PathState state) {
        // Reserve memory for the path segments in our thread-local storage
        if (!pathStorage.IsValueCreated)
            pathStorage.Value = new();

        pathStorage.Value.Reserve((uint)MaxDepth + 1);
        pathStorage.Value.Clear();
    }

    protected override void OnHit(in Ray ray, in Hit hit, ref PathState state) {
        // Prepare the next path segment: set all the info we already have
        var segment = pathStorage.Value.NextSegment();

        // Geometry
        segment.Position = hit.Position;
        segment.DirectionOut = -Vector3.Normalize(ray.Direction);
        segment.Normal = hit.ShadingNormal;

        if (WriteIterationsAsLayers && state.Depth == 1 && GuidingEnabled) {
            var distrib = GetDistribution(-ray.Direction, hit, state);
            if (distrib != null) {
                var region = distrib.Region;

                // Assign a color to this region based on its hash code
                int hash = region.GetHashCode();
                System.Random colorRng = new(hash);
                float hue = (float)colorRng.Next(360);
                float saturation = (float)colorRng.NextDouble() * 0.8f + 0.2f;

                var color = RegionVisualizer.HsvToRgb(hue, saturation, 1.0f);
                iterationCacheVisualizations[scene.FrameBuffer.CurIteration - 1]
                    .Splat(state.Pixel, color);
            }
        }
    }

    protected virtual float ComputeGuidingSelectProbability(Vector3 outDir, in SurfacePoint hit, in PathState state) {
        float roughness = hit.Material.GetRoughness(hit);
        if (roughness < 0.1f) return 0;
        if (hit.Material.IsTransmissive(hit)) return 0;
        return 0.5f;
    }

    protected SurfaceSamplingDistribution GetDistribution(Vector3 outDir, in SurfacePoint hit, in PathState state) {
        SurfaceSamplingDistribution distribution = null;

        if (GuidingEnabled) {
            distribution = distributionBuffer.Value;
            float u = state.Rng.NextFloat();
            distribution.Init(hit.Position, ref u);
            distribution.ApplyCosineProduct(hit.ShadingNormal);
        }
        return distribution;
    }

    protected override (Ray, float, RgbColor, RgbColor) SampleDirection(in SurfaceShader shader, in PathState state) {
        Vector3 outDir = shader.Context.OutDirWorld;
        float selectGuideProb = GuidingEnabled ? ComputeGuidingSelectProbability(outDir, shader.Point, state) : 0;

        SurfaceSamplingDistribution distribution = null;
        if (selectGuideProb > 0) {
            distribution = GetDistribution(outDir, shader.Point, state);
            Debug.Assert(distribution != null);
        }

        Ray nextRay;
        float guidePdf = 0, bsdfPdf;
        RgbColor contrib;
        if (state.Rng.NextFloat() < selectGuideProb) { // sample guided
            var sampledDir = distribution.Sample(state.Rng.NextFloat2D());
            guidePdf = distribution.PDF(sampledDir);
            guidePdf *= selectGuideProb;

            bsdfPdf = shader.Pdf(sampledDir).Pdf;
            bsdfPdf *= (1 - selectGuideProb);

            contrib = shader.EvaluateWithCosine(sampledDir);
            contrib /= guidePdf + bsdfPdf;

            nextRay = Raytracer.SpawnRay(shader.Point, sampledDir);
        } else { // Sample the BSDF (default)
            (nextRay, bsdfPdf, contrib, _) = base.SampleDirection(shader, state);
            bsdfPdf *= (1 - selectGuideProb);

            if (bsdfPdf == 0) { // prevent NaNs / Infs
                return (new(), 0, RgbColor.Black, RgbColor.Black);
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
            return (new(), 0, RgbColor.Black, RgbColor.Black);
        }

        // Update the incident direction and PDF in the current path segment
        var segment = pathStorage.Value.LastSegment;
        var inDir = Vector3.Normalize(nextRay.Direction);
        segment.DirectionIn = inDir;
        segment.PDFDirectionIn = pdf;
        segment.ScatteringWeight = contrib;

        // Material data
        segment.Roughness = shader.GetRoughness();
        if (Vector3.Dot(inDir, outDir) < 0) {
            float ior = shader.GetIndexOfRefractionRatio();
            if (Vector3.Dot(inDir, shader.Context.Normal) < 0) {
                ior = 1 / ior;
            }
            segment.Eta = ior;
        } else {
            segment.Eta = 1;
        }

        return (nextRay, pdf, contrib, contrib);
    }

    protected override float DirectionPdf(in SurfaceShader shader, Vector3 sampledDir,
                                         PathState state) {
        Vector3 outDir = shader.Context.OutDirWorld;
        float selectGuideProb = GuidingEnabled ? ComputeGuidingSelectProbability(outDir, shader.Point, state) : 0;

        if (!GuidingEnabled || selectGuideProb <= 0)
            return base.DirectionPdf(shader, sampledDir, state);

        SurfaceSamplingDistribution distribution = GetDistribution(outDir, shader.Point, state);

        float bsdfPdf = base.DirectionPdf(shader, sampledDir, state) * (1 - selectGuideProb);
        float guidePdf = distribution.PDF(Vector3.Normalize(sampledDir)) * selectGuideProb;
        distribution?.Clear();
        return bsdfPdf + guidePdf;
    }

    protected override void OnNextEventResult(in SurfaceShader shader, in PathState state,
                                             float misWeight, RgbColor estimate) {
        var segment = pathStorage.Value.LastSegment;

        var contrib = misWeight * estimate;
        segment.ScatteredContribution += (Vector3)contrib;
    }

    protected override void OnHitLightResult(in Ray ray, in PathState state, float misWeight, RgbColor emission,
                                             bool isBackground) {
        if (isBackground) {
            // We need to create the path segment first as there was no actual intersection
            var newSegment = pathStorage.Value.NextSegment();
            newSegment.DirectionOut = -Vector3.Normalize(ray.Direction);

            // We move the point far away enough for parallax compensation to no longer make a difference
            newSegment.Position = ray.Origin + ray.Direction * scene.Radius * 1337;
        }

        var segment = pathStorage.Value.LastSegment;
        segment.MiWeight = misWeight;
        segment.DirectContribution = emission;
    }

    protected override void OnFinishedPath(RgbColor estimate, ref PathState state) {
        base.OnFinishedPath(estimate, ref state);

        if (!pathStorage.IsValueCreated) {
            return; // The path never hit anything.
        }

        // Generate the samples and add them to the global cache
        uint num = pathStorage.Value.PrepareSamples(
            useNEEMiWeights: false,
            guideDirectLight: false,
            rrAffectsDirectContribution: true);
        sampleStorage.AddSamples(pathStorage.Value.SamplesRawPointer, num);
    }
}