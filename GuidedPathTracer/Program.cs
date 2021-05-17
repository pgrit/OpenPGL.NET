using System.Diagnostics;
using SeeSharp.Experiments;

namespace GuidedPathTracer {
    class Program {
        static Process ShortTest() {
            SceneRegistry.AddSource("../Scenes");
            Benchmark benchmark = new(new Experiment(16), new() {
                // SceneRegistry.LoadScene("HomeOffice"),
                // SceneRegistry.LoadScene("RoughGlassesIndirect", maxDepth: 10),
                // SceneRegistry.LoadScene("RoughGlasses", maxDepth: 10),

                SceneRegistry.LoadScene("LampCaustic", maxDepth: 10),
                // SceneRegistry.LoadScene("LampCausticNoShade", maxDepth: 10),

                // SceneRegistry.LoadScene("TargetPractice"),
                // SceneRegistry.LoadScene("LivingRoom", maxDepth: 5),
                // SceneRegistry.LoadScene("IndirectRoom", maxDepth: 5),

                // SceneRegistry.LoadScene("CornellBox", maxDepth: 5),
                // SceneRegistry.LoadScene("Pool", maxDepth: 10),
            }, "Results", 1280, 960, SeeSharp.Image.FrameBuffer.Flags.SendToTev);
            benchmark.Run();

            try {
                return Process.Start("python", "./MakeFigure.py Results");
            } catch {
                return Process.Start("python3", "./MakeFigure.py Results");
            }
        }

        static void Main(string[] args) {
            ShortTest().WaitForExit();
        }
    }
}
