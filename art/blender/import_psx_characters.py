"""Convert the locally downloaded Characters PSX pack to Hide iT's character catalog."""

import argparse
import json
import math
import sys
import tempfile
import zipfile
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_prototype as art


ROSTER = {
    'Player_Alex_Orange': 'Character_Killer',
    'Player_Noam_Olive': 'Character_Killer_01',
    'Player_Maya_Denim': 'Character_Killer_03',
    'Player_Eli_Cream': 'Character_Killer_07',
    'Player_Roni_Plum': 'Character_Killer_10',
    'Body_Blue': 'Character_01',
    'Security_Navy': 'Character_18_Police',
    'Guest_Plum': 'Character_Female_03',
    'Civilian_Amir_RustJacket': 'Character_09',
    'Civilian_Ben_CreamShirt': 'Character_11',
    'Civilian_Lior_OliveHoodie': 'Character_16',
    'Civilian_Tamar_RoseCardigan': 'Character_Female_14',
    'Civilian_Nora_NavyCoat': 'Character_Female_13',
    'Civilian_Ruth_TealCoat': 'Character_Female_06',
}

JOINTS = [
    ('Hips', 'Hips', 'Spine', 'Root'),
    ('Spine', 'Spine', 'Spine2', 'Hips'),
    ('Chest', 'Spine2', 'Neck', 'Spine'),
    ('Neck', 'Neck', 'Head', 'Chest'),
    ('Head', 'Head', 'HeadTop_End', 'Neck'),
]
for side in ('Left', 'Right'):
    JOINTS.extend((side+name, side+source, side+tip, side+parent if parent else 'Chest' if name=='Shoulder' else 'Hips')
                  for name, source, tip, parent in (
        ('Shoulder','Shoulder','Arm',None), ('UpperArm','Arm','ForeArm','Shoulder'),
        ('LowerArm','ForeArm','Hand','UpperArm'), ('Hand','Hand','HandIndex1','LowerArm'),
        ('UpperLeg','UpLeg','Leg',None), ('LowerLeg','Leg','Foot','UpperLeg'),
        ('Foot','Foot','ToeBase','LowerLeg'), ('Toes','ToeBase','Toe_End','Foot')))

GRIPS = {
    'GripHead': (0,.020,1.838),
    'GripLeftHand': (.548,-.007,.820), 'GripRightHand': (-.548,-.007,.820),
    'GripLeftAnkle': (.144,0,.159), 'GripRightAnkle': (-.144,0,.159),
}


def joint_name(name):
    return name.rsplit(':', 1)[-1]


def normalized_skin(mesh, canonical):
    names = {source: target for target,source,_,_ in JOINTS}
    names.update({'Spine1':'Spine', 'HeadTop_End':'Head'})
    for side in ('Left','Right'):
        names.update({side+'Toe_End':side+'Toes'})
    source_names = [joint_name(group.name) for group in mesh.vertex_groups]
    skin = []
    for vertex in mesh.data.vertices:
        weights = {}
        for influence in vertex.groups:
            if influence.weight <= 0:
                continue
            source = source_names[influence.group]
            target = names.get(source)
            if target is None and source.startswith(('LeftHand','RightHand')):
                target = 'LeftHand' if source.startswith('Left') else 'RightHand'
            if target not in canonical:
                raise ValueError(f'Unexpected weighted joint {source!r} in {mesh.name}')
            weights[target] = weights.get(target, 0) + influence.weight
        strongest = sorted(weights.items(), key=lambda pair: -pair[1])[:4]
        total = sum(weight for _,weight in strongest)
        if total <= 0:
            raise ValueError(f'Unweighted source vertex {vertex.index} in {mesh.name}')
        skin.append([(name,weight/total) for name,weight in strongest])
    return skin


def fit_live_rig(rig, positions):
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    for name, (head, tip) in positions.items():
        bone = rig.data.edit_bones[name]
        bone.head = head
        bone.tail = tip
        bone.roll = 0
    bpy.ops.object.mode_set(mode='OBJECT')


def adapt_body(points, skin, original, target):
    poses = {}
    limb = ('UpperArm','LowerArm','Hand','UpperLeg','LowerLeg','Foot','Toes')
    for name, (start, tip) in original.items():
        bone = target.data.bones[name]
        direction = tip-start
        destination = bone.tail_local-bone.head_local
        bend = name.endswith(limb)
        rotation = direction.rotation_difference(destination) if bend else None
        scale = min(1.26,max(.74,destination.length/direction.length)) if bend else 1
        poses[name] = start,bone.head_local,rotation,direction.normalized(),scale
    result = []
    for point, weights in zip(points, skin):
        value = Vector()
        for name, weight in weights:
            start, head, rotation, axis, scale = poses[name]
            offset = point-start
            if rotation is not None:
                offset += axis * offset.dot(axis) * (scale-1)
                offset = rotation @ offset
            value += (head+offset)*weight
        result.append(value)
    return result


def marker(rig, name, position):
    obj = bpy.data.objects.new(f'{rig.name.removesuffix("_Rig")}__{name}', None)
    bpy.context.collection.objects.link(obj)
    obj.parent = rig
    obj.location = position
    return obj


def build_character(game_id, input_path, texture, output):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(input_path),use_anim=False,use_image_search=False)
    imported = list(bpy.context.scene.objects)
    source = next(obj for obj in imported if obj.type=='ARMATURE')
    mesh, = (obj for obj in imported if obj.type=='MESH')
    source_joints = {joint_name(b.name):source.matrix_world@b.head_local for b in source.data.bones}
    points = [mesh.matrix_world@v.co for v in mesh.data.vertices]
    floor = min(p.z for p in points)
    height = max(p.z for p in points)-floor
    center = source_joints['Hips']
    origin = Vector((center.x,center.y,floor))
    # The pack faces +X. Hide iT's Blender exports face -Y with left at +X.
    align = Matrix.Rotation(-math.pi/2,3,'Z')
    scale = 1.79/height
    joints = {name:align@((point-origin)*scale) for name,point in source_joints.items()}
    points = [align@((point-origin)*scale) for point in points]
    positions = {}
    for name, source_name, tail, parent in JOINTS:
        head = joints[source_name]
        tip = joints.get(tail)
        if tip is None or (tip-head).length < .001:
            tip = head+(head-positions[parent][0]).normalized()*.06
        positions[name] = head,tip

    rig = art.make_rig(game_id)
    skin = normalized_skin(mesh, positions)
    is_body = game_id == 'Body_Blue'
    if is_body:
        points = adapt_body(points,skin,positions,rig)
    else:
        fit_live_rig(rig,positions)
    mesh.parent = None
    mesh.matrix_world = Matrix.Identity(4)
    for modifier in list(mesh.modifiers):
        mesh.modifiers.remove(modifier)
    for obj in imported:
        if obj != mesh:
            bpy.data.objects.remove(obj,do_unlink=True)
    mesh.name = game_id+'_Mesh'
    mesh.data.name = mesh.name
    mesh.vertex_groups.clear()
    groups = {}
    for vertex, point, weights in zip(mesh.data.vertices,points,skin):
        vertex.co = point
        for name, weight in weights:
            if name not in groups:
                groups[name] = mesh.vertex_groups.new(name=name)
            groups[name].add([vertex.index],weight,'REPLACE')
    mesh.parent = rig
    modifier = mesh.modifiers.new('Hide iT skin','ARMATURE')
    modifier.object = rig

    material = bpy.data.materials.new(game_id+'_PSX')
    material.use_nodes = True
    shader = material.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Roughness'].default_value = .86
    image = material.node_tree.nodes.new('ShaderNodeTexImage')
    image.image = bpy.data.images.load(str(texture),check_existing=True)
    image.interpolation = 'Closest'
    material.node_tree.links.new(image.outputs['Color'],shader.inputs['Base Color'])
    mesh.data.materials.clear()
    mesh.data.materials.append(material)
    for face in mesh.data.polygons:
        face.material_index = 0
    mesh.data.update()

    markers = [marker(rig,'FaceForward',(0,-.40,1.69))]
    if is_body:
        markers.extend(marker(rig,name,local) for name,local in GRIPS.items())
    bpy.ops.object.select_all(action='DESELECT')
    for obj in [rig,mesh,*markers]:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.fbx(filepath=str(output),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},
        global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
        use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,
        path_mode='RELATIVE',axis_forward='-Z',axis_up='Y')
    if is_body:
        # Preserve the local rigged source without adding another copy to any distribution.
        source_path = art.PROJECT/'.tools'/'CharactersPSX'/'Hide-iT-PSX-Body.blend'
        source_path.parent.mkdir(parents=True,exist_ok=True)
        image.image.pack()
        bpy.data.orphans_purge(do_recursive=True)
        bpy.ops.wm.save_as_mainfile(filepath=str(source_path))
    return {'source':input_path.stem, 'triangles':sum(len(face.vertices)-2 for face in mesh.data.polygons),
            'bones':len(rig.data.bones),'height_m':round(max(p.z for p in points)-min(p.z for p in points),4)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--pack',required=True,type=Path,help='Path to the official Characters_psx_1.2.zip')
    options = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    destination = art.EXPORT/'CharactersPSX'
    for folder in ('Models','Textures'):
        (destination/folder).mkdir(parents=True,exist_ok=True)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1
    bpy.context.preferences.filepaths.save_version = 0
    report = {}
    with zipfile.ZipFile(options.pack) as archive, tempfile.TemporaryDirectory() as work:
        files = {Path(entry).stem:entry for entry in archive.namelist() if '/Models/Rig/' in entry and entry.endswith('.fbx')}
        textures = {Path(entry).stem:entry for entry in archive.namelist() if '/Textures/' in entry and entry.endswith('.png')}
        for game_id, source in ROSTER.items():
            fbx = Path(work)/f'{source}.fbx'
            fbx.write_bytes(archive.read(files[source]))
            texture = destination/'Textures'/f'{game_id}.png'
            texture.write_bytes(archive.read(textures[source]))
            report[game_id] = build_character(game_id,fbx,texture,destination/'Models'/f'{game_id}.fbx')
    (art.ART/'psx-character-manifest.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(f'HIDEIT_PSX_IMPORT_OK: {len(report)} characters')


if __name__ == '__main__':
    main()
