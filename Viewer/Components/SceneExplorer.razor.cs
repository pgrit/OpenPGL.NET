using Microsoft.AspNetCore.Components;
using SeeSharp.Integrators;
using SeeSharp.Integrators.Util;
using System;
using System.Numerics;

namespace Viewer.Components {
    public partial class SceneExplorer : ProbeImage {
        [Parameter]
        public Vector3 CurrentPoint { get; set; }

        [Parameter]
        public Vector3 CurrentDirection { get; set; }

        [Parameter]
        public float ArrowLength { get; set; }

        [Parameter]
        public float ArrowRadius { get; set; }

        [Parameter]
        public bool ShowMarker { get; set; }

        public int Yaw {
            get => yaw;
            set {
                yaw = value;
                RenderImage();
            }
        }
        int yaw = 0;

        public int Pitch {
            get => pitch;
            set {
                pitch = value;
                RenderImage();
            }
        }
        int pitch = 0;

        protected override void OnParametersSet() {
            base.OnParametersSet();
            RenderImage();
        }

        void RenderImage() {
            if (Scene == null) return;

            DebugVisualizer vis;
            if (ShowMarker) {
                vis = new PathVisualizer() {
                    TotalSpp = 1,
                    TypeToColor = new() {
                        {0, new(1,0,0)}
                    },
                    Paths = new() {
                        new() {
                            Vertices = new() { CurrentPoint, CurrentPoint + CurrentDirection * ArrowLength},
                            UserTypes = new() { 0, 0 }
                        }
                    },
                    Radius = ArrowRadius
                };
            } else {
                vis = new() { TotalSpp = 1 };
            }

            lock (Scene) {
                var old = Scene.Camera.WorldToCamera;
                var rot = Matrix4x4.CreateFromYawPitchRoll(yaw * MathF.PI / 180.0f,
                    pitch * MathF.PI / 180.0f, 0);
                Scene.Camera.WorldToCamera = Scene.Camera.WorldToCamera * rot;

                Scene.FrameBuffer.Reset();
                vis.Render(Scene);
                previewImageData = Scene.FrameBuffer.Image.AsBase64Png();

                Scene.Camera.WorldToCamera = old;
            }
            StateHasChanged();
        }

        protected override void OnInitialized() {
            base.OnInitialized();

            RenderImage();
        }
    }
}