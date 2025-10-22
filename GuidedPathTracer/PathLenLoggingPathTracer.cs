using SeeSharp.Images;
using SeeSharp.Integrators;
using SimpleImageIO;

namespace GuidedPathTracer {
    public class PathLenLoggingPathTracer : PathTracer {
        MonoLayer avgPathLenLayer;

        protected override void OnPrepareRender() {
            avgPathLenLayer = new();
            scene.FrameBuffer.AddLayer("avglen", avgPathLenLayer);
        }

        protected override void OnFinishedPath(RgbColor estimate, ref PathState state) {
            avgPathLenLayer.Splat(state.Pixel, state.Depth);
        }
    }
}