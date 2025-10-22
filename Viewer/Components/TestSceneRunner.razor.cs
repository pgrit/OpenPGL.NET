using System;
using System.Numerics;
using System.Threading.Tasks;
using GuidedPathTracer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OpenPGL.NET;
using SeeSharp.Cameras;
using SeeSharp.Experiments;
using SeeSharp.Geometry;
using SeeSharp.Images;
using SeeSharp.Integrators.Bidir;
using SeeSharp.Sampling;
using SeeSharp.Shading;
using SimpleImageIO;
using TinyEmbree;

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
        RgbImage probeImage;
        RgbImage regionVisImage;
        Vector3 queryPoint;
        Vector3 selectedDirection;

        float arrowRadius = 0.01f;
        float arrowLength = 0.5f;

        protected int ProbePixelX { get; set; }
        protected int ProbePixelY { get; set; }

        int resolution = 640;
        Vector3 upVector = Vector3.UnitY;

        float exposure {
            get => curExposure;
            set {
                lastExposure = curExposure;
                curExposure = value;
                if (curExposure != lastExposure) {
                    float newScale = MathF.Pow(2.0f, curExposure);
                    float oldScale = MathF.Pow(2.0f, lastExposure);
                    float correction = newScale / oldScale;

                    renderResult?.Scale(correction);
                    probeImage?.Scale(correction);
                }
            }
        }
        float curExposure = 0;
        float lastExposure = 0;

        async Task Run() {
            running = true;

            StateHasChanged();

            // Notify our parent that the user triggered a run with a potentially modified setup
            SetupChangedCallback?.Invoke();

            await Task.Run(() => {
                var sceneLoader = new SceneFromFile(Setup.ScenePath, 0, Setup.MaxDepth);
                Scene = sceneLoader.MakeScene();
                Scene.FrameBuffer = new(640, 480, "");
                Scene.Prepare();

                integrator = new() {
                    TotalSpp = Setup.NumSamples,
                    MaxDepth = Setup.MaxDepth,
                    SpatialSettings = new KdTreeSettings() { KnnLookup = false }
                };

                integrator.Render(Scene);

                renderResult = Scene.FrameBuffer.Image;
                renderResult.Scale(MathF.Pow(2.0f, curExposure));
            });

            await Task.Run(() => regionVisImage = MakeRegionVisImage());

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
                    dir = ShadingSpace.ShadingToWorld(upVector, dir);

                    float pdf = distribution.PDF(dir) * MathF.Sin(sphericalDir.Y);
                    image.SetPixel(col, row, new(pdf, pdf, pdf));
                }
            }
            return image;
        }

        RgbImage MakeProbeImage(Ray ray, SurfacePoint point, int resolution) {
            var scene = Scene.Copy();
            scene.FrameBuffer = new(resolution, resolution, "");

            // Make sure the normal is on the right side of the surface
            var normal = point.Normal;
            if (Vector3.Dot(point.Normal, -ray.Direction) < 0)
                normal *= -1;

            scene.Camera = new LightProbeCamera(point.Position, normal, point.ErrorOffset, upVector);
            scene.Prepare();

            VertexConnectionAndMerging integrator = new() {
                NumIterations = 1,
                RenderTechniquePyramid = false,
                MaxDepth = Setup.MaxDepth
            };
            integrator.Render(scene);

            return RgbImage.StealData(scene.FrameBuffer.GetLayer("denoised").Image);
        }

        RgbImage MakeRegionVisImage() {
            var scene = Scene.Copy();
            scene.FrameBuffer = new(640, 480, "");
            scene.Prepare();

            RegionVisualizer visualizer = new() {
                GuidingField = integrator.GuidingField
            };
            visualizer.Render(scene);

            return RgbImage.StealData(scene.FrameBuffer.Image);
        }

        async Task Query(int col, int row, Ray ray, SurfacePoint point) {
            // Query the guiding cache at the given position
            using (SurfaceSamplingDistribution distrib = new(integrator.GuidingField)) {
                var u = 0.5f;
                distrib.Init(point.Position, ref u);
                await Task.Run(() => distributionImage = MakeDistributionImage(distrib, resolution));
            }

            await Task.Run(() => {
                probeImage = MakeProbeImage(ray, point, resolution);
                probeImage.Scale(MathF.Pow(2.0f, curExposure));
            });

            queryPoint = point.Position;
            selectedDirection = Vector3.UnitZ;

            StateHasChanged();
        }

        void UpdateProbePixel(MouseEventArgs e) {
            ProbePixelX = (int)e.OffsetX;
            ProbePixelY = (int)e.OffsetY;

            Vector2 sphericalDir = new(
                ProbePixelX / (float)resolution * 2.0f * MathF.PI,
                ProbePixelY / (float)resolution * MathF.PI
            );
            selectedDirection = SampleWarp.SphericalToCartesian(sphericalDir);
            selectedDirection = ShadingSpace.ShadingToWorld(upVector, selectedDirection);

            StateHasChanged();
        }
    }
}