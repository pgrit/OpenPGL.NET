using System.Collections.Generic;
using OpenPGL.NET;
using SeeSharp.Integrators.Bidir;

namespace GuidedPathTracer {
    public class Experiment : SeeSharp.Experiments.Experiment {
        int numSamples = 16;

        public Experiment(int numSamples) => this.numSamples = numSamples;

        public override List<Method> MakeMethods() => new() {
            new("PathTracer", new PathLenLoggingPathTracer() {
              TotalSpp = numSamples,
              NumShadowRays = 1,
            }),
            new("Guided", new GuidedPathTracer() {
                TotalSpp = numSamples,
                NumShadowRays = 1,
                SpatialSettings = new KdTreeSettings() { KnnLookup = false }
            }),
            new("GuidedKnn", new GuidedPathTracer() {
                TotalSpp = numSamples,
                NumShadowRays = 1,
                SpatialSettings = new KdTreeSettings() { KnnLookup = true }
            }),
            new("Vcm", new VertexConnectionAndMerging() {
               NumIterations = numSamples / 2,
            })
        };
    }
}