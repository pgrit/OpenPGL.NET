using SeeSharp.Image;
using SeeSharp.Integrators;

namespace GuidedPathTracer {
    public class PathLenLoggingPathTracer : PathTracer {
        MonoLayer avgPathLenLayer;

        protected override void OnPrepareRender() {
            avgPathLenLayer = new();
            scene.FrameBuffer.AddLayer("avglen", avgPathLenLayer);
        }

        protected override void OnFinishedPath(RadianceEstimate estimate, PathState state) {
            avgPathLenLayer.Splat(state.Pixel.X, state.Pixel.Y, state.Depth);
        }
    }
}