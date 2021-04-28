using System.Diagnostics;
using SeeSharp.Experiments;

namespace GuidedPathTracer {
    class Program {
        static Process ShortTest() {
            SceneRegistry.AddSource("../Scenes");
            Benchmark benchmark = new(new Experiment(32), new() {
                // SceneRegistry.LoadScene("HomeOffice"),
                SceneRegistry.LoadScene("CornellBox"),
                // SceneRegistry.LoadScene("RoughGlassesIndirect", maxDepth: 10),
                // SceneRegistry.LoadScene("GlassBall", maxDepth: 10),
            }, "Results", 640, 480);
            benchmark.Run();

            try {
                return Process.Start("python", "./LpmFigure.py Results");
            } catch {
                return Process.Start("python3", "./LpmFigure.py Results");
            }
        }

        static void Main(string[] args) {
            ShortTest().WaitForExit();
        }
    }
}
