using System.Collections.Generic;
using OpenPGL.NET;
using SeeSharp.Integrators.Bidir;

namespace GuidedPathTracer;

public class Experiment : SeeSharp.Experiments.Experiment {
    int numSamples = 16;
    int maxTime = int.MaxValue;

    public Experiment(int numSamples, int maxTime = int.MaxValue) {
        this.numSamples = numSamples;
        this.maxTime = maxTime;
    }

    public override List<Method> MakeMethods() => new() {
        new("PathTracer", new PathLenLoggingPathTracer() {
            TotalSpp = numSamples,
            MaximumRenderTimeMs = maxTime,
            NumShadowRays = 1,
        }),
        new("Guided", new GuidedPathTracer() {
            TotalSpp = numSamples,
            MaximumRenderTimeMs = maxTime,
            NumShadowRays = 1,
            SpatialSettings = new KdTreeSettings() { KnnLookup = false },
            WriteIterationsAsLayers = false,
        }),
        new("GuidedKnn", new GuidedPathTracer() {
            TotalSpp = numSamples,
            MaximumRenderTimeMs = maxTime,
            NumShadowRays = 1,
            SpatialSettings = new KdTreeSettings() { KnnLookup = true }
        }),
        new("Vcm", new VertexConnectionAndMerging() {
           NumIterations = numSamples / 2,
           MaximumRenderTimeMs = maxTime,
        })
    };
}