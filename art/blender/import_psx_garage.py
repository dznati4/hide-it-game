"""Convert the two Zenthrall free samples into local-only Unity resources."""
import argparse
import hashlib
from pathlib import Path
import re
import shutil
import sys
import tempfile
import zipfile

import bpy


CITY_MODELS = (
    "PSX_Commercial_PizzaRestaurant", "PSX_Prop_StopSign_StreetNames",
    "PSX_Prop_Streetlight_CobraHead", "PSX_Prop_FireHydrant", "PSX_Debris_TrashBag_Full",
)
CITY_TEXTURES = ("Asphalt_Road", "Concrete_Weathered", "Concrete_Sidewalk",
                 "Brick_Brown_Apartment", "Paint_Green_Dumpster")
SUBMARINE_MODELS = (
    "SM_Bulkhead_Door", "SM_Station_Fuse_Box", "SM_Locker", "SM_Light_Wall_Caged",
    "SM_Pipe_Straight_1m", "SM_Pipe_Elbow90",
)
SUBMARINE_TEXTURES = ("T_FloorPlate_A", "T_PaintOlive_B_Rusted")


def copy_file(archive, filename, destination):
    matches = [entry for entry in archive.namelist() if Path(entry).name == filename]
    if len(matches) != 1:
        raise ValueError(f"Expected exactly one {filename} in {archive.filename}")
    with archive.open(matches[0]) as source, destination.open("wb") as output:
        shutil.copyfileobj(source, output)


def convert_model(source, models, textures):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source))
    used_materials = {slot.material for obj in bpy.context.scene.objects if obj.type == "MESH"
                      for slot in obj.material_slots if slot.material is not None}
    by_image = {}
    for material in sorted(used_materials, key=lambda item: item.name):
        if not material.use_nodes:
            continue
        shader = next((node for node in material.node_tree.nodes if node.type == "BSDF_PRINCIPLED"), None)
        if shader is None:
            continue
        links = shader.inputs["Base Color"].links
        if links and links[0].from_node.type == "TEX_IMAGE":
            image = links[0].from_node.image
            if image is None or image.packed_file is None:
                continue
            digest = hashlib.sha256(image.packed_file.data).hexdigest()[:16]
            existing = by_image.get(digest)
            if existing is not None:
                material.user_remap(existing)
                continue
            by_image[digest] = material
            material.name = "GarageTex_" + digest
            target = textures / (material.name + ".png")
            if not target.exists():
                image.file_format = "PNG"
                image.save(filepath=str(target))
    for obj in bpy.context.scene.objects:
        obj.animation_data_clear()
    bpy.ops.export_scene.fbx(filepath=str(models / (source.stem + ".fbx")),
                             object_types={"MESH", "EMPTY"}, bake_anim=False,
                             use_armature_deform_only=True, add_leaf_bones=False)


def import_pack(path, model_names, texture_names, models, textures):
    with tempfile.TemporaryDirectory(prefix="hideit-garage-") as tmp, zipfile.ZipFile(path) as archive:
        for name in model_names:
            source = Path(tmp) / (name + ".glb")
            copy_file(archive, source.name, source)
            convert_model(source, models, textures)
        for name in texture_names:
            filename = name + ".png"
            copy_file(archive, filename, textures / filename)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--city", required=True, type=Path)
    parser.add_argument("--submarine", required=True, type=Path)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    root = Path(__file__).resolve().parents[2] / "Assets/HideIT/Resources/GarageKit"
    local_textures = root / "Textures"
    with tempfile.TemporaryDirectory(prefix="hideit-garage-stage-") as tmp:
        models, textures = Path(tmp) / "Models", Path(tmp) / "Textures"
        models.mkdir()
        textures.mkdir()
        import_pack(args.city, CITY_MODELS, CITY_TEXTURES, models, textures)
        import_pack(args.submarine, SUBMARINE_MODELS, SUBMARINE_TEXTURES, models, textures)
        shutil.copytree(models, root / "Models", dirs_exist_ok=True)
        shutil.copytree(textures, local_textures, dirs_exist_ok=True)
        for name in CITY_MODELS + SUBMARINE_MODELS:
            for old in local_textures.glob(name + "_*.png"):
                if re.fullmatch(re.escape(name) + r"_\d+\.png", old.name):
                    old.unlink()
                    old.with_name(old.name + ".meta").unlink(missing_ok=True)
    print(f"Imported {len(CITY_MODELS) + len(SUBMARINE_MODELS)} PSX garage models to {root}")


if __name__ == "__main__":
    main()
