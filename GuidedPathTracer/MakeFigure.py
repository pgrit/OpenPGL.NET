import figuregen
import simpleimageio as sio
from figuregen.util.templates import FullSizeWithCrops
from figuregen.util.image import Cropbox, lin_to_srgb, exposure
import os
import json

method_names = [ "PathTracer", "Guided" ] #, "GuidedKnn", "Vcm" ]

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
            with open(m) as f:
                meta = json.load(f)
            render_times.append((meta["NumIterations"], meta["RenderTime"]))

        for i in range(len(method_names)):
            c = f"{method_names[i]} ({render_times[i][0]}spp after {render_times[i][1] / 1000:.1f}s)"
            method_captions[i + 1] = c

        super().__init__(
            sio.read(os.path.join(scene_folder, "Reference.exr")),
            [
                sio.read(os.path.join(scene_folder, name, "Render.exr"))
                for name in method_names
            ], crops, True, method_captions, False)

    def tonemap(self, img):
        return figuregen.JPEG(lin_to_srgb(exposure(img, self._exposure)), quality=80)

figure_rows = []

scene_filter = ["HomeOffice", "ModernLivingRoom", "Pool"]

scene_exposures = {
    "Pool": -3.0,
    "RoughGlassesIndirect": 2,
    "HomeOffice": 1,
    "LampCaustic": 0.0,
    "LampCausticNoShade": 0.0,
}

scene_crops = {
    # "Pool": [
    #     Cropbox(top=500, left=530, height=160, width=220, scale=5),
    #     Cropbox(top=200, left=1030, height=160, width=220, scale=5)
    # ],
    # "RoughGlasses": [
    #     Cropbox(top=700, left=150, height=160, width=220, scale=5),
    #     Cropbox(top=500, left=400, height=160, width=220, scale=5)
    # ],
    # "LampCaustic": [
    #     Cropbox(top=700, left=150, height=160, width=220, scale=5),
    #     Cropbox(top=200, left=650, height=160, width=220, scale=5)
    # ],
    # "LampCausticNoShade": [
    #     Cropbox(top=700, left=150, height=160, width=220, scale=5),
    #     Cropbox(top=700, left=850, height=160, width=220, scale=5)
    # ],
    "default": [
        Cropbox(top=(int)(0.5*200), left=(int)(0.5*230), height=(int)(0.5*160), width=(int)(0.5*220), scale=5),
        Cropbox(top=(int)(0.5*200), left=(int)(0.5*430), height=(int)(0.5*160), width=(int)(0.5*220), scale=5)
    ]
}

for dirname in os.listdir("Results"):
    scene_folder = os.path.join("Results", dirname)
    if not os.path.isdir(scene_folder): continue
    if scene_filter is not None and dirname not in scene_filter: continue

    rows = FigureLayout(
        scene_folder,
        crops=scene_crops[dirname] if dirname in scene_crops else scene_crops["default"],
        exposure=scene_exposures[dirname] if dirname in scene_exposures else 0,
    ).figure
    figure_rows.extend(rows)

figuregen.figure(figure_rows, 18, "Results/Overview.pdf")
