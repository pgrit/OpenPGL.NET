import figuregen
import simpleimageio as sio
from figuregen.util.templates import FullSizeWithCrops
from figuregen.util.image import Cropbox, lin_to_srgb, exposure
import os
import json

method_names = [ "PathTracer", "Guided", "Vcm" ]

class FigureLayout(FullSizeWithCrops):
    def __init__(self, scene_folder, exposure, crops):
        self._exposure = exposure
        method_captions = ["Reference"]
        method_captions.extend(method_names)

        # Read the meta data file and get the render time
        meta_filenames = [
            os.path.join(scene_folder, name, "Render.json")
            for name in method_names
        ]
        render_times = []
        for m in meta_filenames:
            with open(m) as f: render_times.append(json.load(f)["RenderTime"])

        for i in range(len(method_names)):
            method_captions[i + 1] = f"{method_names[i]} ({render_times[i] / 1000:.1f}s)"

        super().__init__(
            sio.read(os.path.join(scene_folder, "Reference.exr")),
            [
                sio.read(os.path.join(scene_folder, name, "Render.exr"))
                for name in method_names
            ], crops, True, method_captions, False)

    def tonemap(self, img):
        return figuregen.JPEG(lin_to_srgb(exposure(img, self._exposure)), quality=80)

figure_rows = []

scene_exposures = {
    "Pool": -2.5,
    "RoughGlassesIndirect": 2,
    "HomeOffice": 1
}

for dirname in os.listdir("Results"):
    scene_folder = os.path.join("Results", dirname)
    if not os.path.isdir(scene_folder): continue

    rows = FigureLayout(
        scene_folder,
        crops=[
            Cropbox(top=200, left=230, height=80, width=110, scale=5),
            Cropbox(top=200, left=430, height=80, width=110, scale=5)
        ],
        exposure=scene_exposures[dirname] if dirname in scene_exposures else 0,
    ).figure
    figure_rows.extend(rows)

figuregen.figure(figure_rows, 18, "Results/Overview.pdf")
