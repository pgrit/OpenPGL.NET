using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OpenPGL.NET;
using SeeSharp.Experiments;
using SeeSharp.Sampling;
using SimpleImageIO;

namespace Viewer.Components {
    public partial class TestSceneRunner : ComponentBase {
        public class CaseInfo {
            public string ScenePath { get; set; }
            public int NumSamples { get; set; }
            public int MaxDepth { get; set; }
        }

        [Parameter]
        public CaseInfo Setup { get; set; }

        public delegate void SetupCallback();

        [Parameter]
        public SetupCallback SetupChangedCallback { get; set; }

        public SeeSharp.Scene Scene;
        bool running = false;
        GuidedPathTracer.GuidedPathTracer integrator;
        RgbImage renderResult;

        RgbImage distributionImage;
        Vector3 queryPoint;
        Vector3 selectedDirection;

        float arrowRadius = 0.01f;
        float arrowLength = 0.5f;

        async Task Run() {
            running = true;
            StateHasChanged();
            SetupChangedCallback?.Invoke();

            // TODO remove hack
            Setup.ScenePath = "../Scenes/CornellBox/CornellBox.json";

            await Task.Run(() => {
                var sceneLoader = new SceneFromFile(Setup.ScenePath, Setup.MaxDepth);
                Scene = sceneLoader.MakeScene();
                Scene.FrameBuffer = new(640, 480, "");
                Scene.Prepare();

                integrator = new() {
                    TotalSpp = Setup.NumSamples
                };

                integrator.Render(Scene);

                renderResult = Scene.FrameBuffer.Image;
            });

            running = false;
            StateHasChanged();
        }

        RgbImage MakeDistributionImage(SurfaceSamplingDistribution distribution, int resolution) {
            RgbImage image = new(resolution, resolution);
            for (int row = 0; row < resolution; ++row) {
                for (int col = 0; col < resolution; ++col) {
                    Vector2 sphericalDir = new(
                        col / (float)resolution * 2.0f * MathF.PI,
                        row / (float)resolution * MathF.PI
                    );
                    Vector3 dir = SampleWarp.SphericalToCartesian(sphericalDir);
                    float pdf = distribution.PDF(dir) * MathF.Sin(sphericalDir.Y);
                    image.SetPixel(col, row, new(pdf, pdf, pdf));
                }
            }
            return image;
        }

        async Task Query(int col, int row, Vector3 pos) {
            // Query the guiding cache at the given position
            System.Random rng = new(1337);
            SamplerWrapper sampler = new(
                () => (float)rng.NextDouble(),
                () => new((float)rng.NextDouble(), (float)rng.NextDouble())
            );

            var region = integrator.GuidingField.GetSurfaceRegion(pos, sampler);
            if (!region.IsValid)
                Console.WriteLine("Invalid scene region selected.");

            using (SurfaceSamplingDistribution distrib = new()) {
                distrib.Init(region, pos);
                if (!distrib.IsValid)
                    Console.WriteLine("Invalid distribution.");

                distributionImage = MakeDistributionImage(distrib, resolution);
                queryPoint = pos;
                selectedDirection = Vector3.UnitZ;
            }

            StateHasChanged();
        }

        protected int ProbePixelX { get; set; }
        protected int ProbePixelY { get; set; }
        int resolution = 640;

        void UpdateProbePixel(MouseEventArgs e) {
            ProbePixelX = (int)e.OffsetX;
            ProbePixelY = (int)e.OffsetY;

            Vector2 sphericalDir = new(
                ProbePixelX / (float)resolution * 2.0f * MathF.PI,
                ProbePixelY / (float)resolution * MathF.PI
            );
            selectedDirection = SampleWarp.SphericalToCartesian(sphericalDir);

            StateHasChanged();
        }
    }
}