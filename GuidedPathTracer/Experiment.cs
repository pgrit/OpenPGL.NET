using System.Collections.Generic;

namespace GuidedPathTracer {
    public class Experiment : SeeSharp.Experiments.Experiment {
        int numSamples = 16;

        public Experiment(int numSamples) => this.numSamples = numSamples;

        public override List<Method> MakeMethods() => new() {
            new("PathTracer", new SeeSharp.Integrators.PathTracer() {
                TotalSpp = numSamples,
                NumShadowRays = 1
            }),
            new("Guided", new GuidedPathTracer() {
                TotalSpp = numSamples,
                NumShadowRays = 1,
            })
        };
    }
}