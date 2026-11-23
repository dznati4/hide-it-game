"""Original Hide iT asset study. Run with Blender --background --python this_file.

The body uses MakeHuman CC0 facial geometry. Its clothes, hair, and textures are authored here.
The source .blend stays outside Unity/Assets; only explicit FBX exports are imported.
"""
import bpy
import math
import json
import sys
from pathlib import Path
from mathutils import Vector, Quaternion
import bmesh

ART = Path(__file__).resolve().parents[1]
PROJECT = ART.parent
EXPORT = PROJECT / 'Assets' / 'HideIT' / 'ArtPrototype'
PREVIEW = ART / 'previews'
SOURCE = ART / 'source'
for directory in (EXPORT / 'Models', EXPORT / 'Textures', PREVIEW, SOURCE):
    directory.mkdir(parents=True, exist_ok=True)

PALETTE = {
    'ink': '#172B36', 'navy': '#294759', 'teal': '#168A86', 'mint': '#9FD9C8',
    'orange': '#E98645', 'cream': '#F2E7CE', 'plum': '#795C87', 'mustard': '#DBB65E',
    'skin_light': '#E4B294', 'skin_mid': '#BE8466', 'skin_dark': '#815942', 'hair': '#3A3034',
    'white': '#F8F3E9', 'slate': '#708A92', 'concrete': '#A4B3B0', 'wood': '#B77E58',
    'rubber': '#283740', 'glass': '#86B3BB', 'blue': '#6D98B6', 'rose': '#CB8197',
    'warning': '#E6AA4C', 'alert': '#D46458', 'floor': '#596D73', 'dark_wood': '#755B4D',
    'olive': '#707B58', 'denim': '#506F92', 'eye_white': '#E6E0D5', 'eye_dark': '#273136',
    'brass': '#B89C5F', 'lip': '#AD796D', 'hair_brown': '#654C3F', 'rust': '#BA7050',
    'grey_hair': '#A29E95',
    'body_fleece': '#29282D', 'body_ribbing': '#222127', 'body_fleece_edge': '#46434A',
    'body_jersey': '#C0BDBA', 'body_jersey_trim': '#ABA7A5', 'body_denim': '#40516D',
    'body_denim_seam': '#68728A', 'body_canvas': '#D6CEBF', 'body_sole': '#ACA395',
    'body_hair': '#34251F', 'body_hair_light': '#423028', 'body_beard': '#4C352C',
    'skin_body': '#BA886C', 'body_lids': '#784F45', 'body_lips': '#A06E62', 'body_nail': '#B88478',
}
COLORS = list(PALETTE)
MANIFEST = {'title': 'Hide iT', 'brand': 'ZanettiZONEai', 'blood': False,
            'units': 'meters', 'assets': [], 'palette': PALETTE, 'rig_bones': []}

def rgba(key):
    value = PALETTE.get(key, key).lstrip('#')
    return tuple(int(value[i:i+2], 16) / 255.0 for i in (0, 2, 4)) + (1.0,)

def reset():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for data in list(bpy.data.materials):
        bpy.data.materials.remove(data)

def make_palette():
    image = bpy.data.images.new('HIdeIT_Palette', width=128, height=128, alpha=False)
    pixels = []
    for y in range(128):
        for x in range(128):
            index = (y // 16) * 8 + (x // 16)
            pixels.extend(rgba(COLORS[index]) if index < len(COLORS) else rgba('ink'))
    image.pixels = pixels
    image.filepath_raw = str(EXPORT / 'Textures' / 'HIdeIT_Palette.png')
    image.file_format = 'PNG'
    image.save()
    mask=bpy.data.images.new('HIdeIT_SurfaceMask',width=128,height=128,alpha=True)
    mask.colorspace_settings.name='Non-Color'
    mask_pixels=[]
    for y in range(128):
        for x in range(128):
            index=(y//16)*8+x//16
            key=COLORS[index] if index<len(COLORS) else 'ink'
            roughness=.52 if key.startswith('skin_') else .20 if key.startswith('eye_') else .88 if key=='rubber' else .42 if key=='brass' else .82
            mask_pixels.extend((.65 if key=='brass' else 0,0,0,1-roughness))
    mask.pixels=mask_pixels
    mask.filepath_raw=str(EXPORT/'Textures'/'HIdeIT_SurfaceMask.png')
    mask.file_format='PNG'
    mask.save()
    material = bpy.data.materials.new('HIdeIT_Palette')
    material.use_nodes = True
    shader = material.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Roughness'].default_value = .76
    texture = material.node_tree.nodes.new('ShaderNodeTexImage')
    texture.image = image
    texture.interpolation = 'Closest'
    material.node_tree.links.new(texture.outputs['Color'], shader.inputs['Base Color'])
    surface=material.node_tree.nodes.new('ShaderNodeTexImage')
    surface.image=mask
    surface.interpolation='Closest'
    separate=material.node_tree.nodes.new('ShaderNodeSeparateColor')
    material.node_tree.links.new(surface.outputs['Color'],separate.inputs[0])
    material.node_tree.links.new(separate.outputs[0],shader.inputs['Metallic'])
    inverse=material.node_tree.nodes.new('ShaderNodeMath')
    inverse.operation='SUBTRACT'
    inverse.inputs[0].default_value=1
    material.node_tree.links.new(surface.outputs['Alpha'],inverse.inputs[1])
    material.node_tree.links.new(inverse.outputs[0],shader.inputs['Roughness'])
    return material

def finish(obj, name, color, bone=None):
    obj.name = name
    obj.data.materials.clear()
    obj.data.materials.append(ATLAS)
    uv = obj.data.uv_layers.active or obj.data.uv_layers.new(name='PaletteUV')
    uv.name = 'PaletteUV'
    uv.active_render = True
    index = COLORS.index(color)
    coord = ((index % 8 + .5) / 8, (index // 8 + .5) / 8)
    for loop in uv.data:
        loop.uv = coord
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bone:
        group = obj.vertex_groups.new(name=bone)
        group.add(list(range(len(obj.data.vertices))), 1.0, 'REPLACE')
    return obj

def box(name, position, size, color, bevel=.02, bone=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=position)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        modifier = obj.modifiers.new('Soft silhouette', 'BEVEL')
        modifier.width = min(bevel, min(size) * .28)
        modifier.segments = 2
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    return finish(obj, name, color, bone)

def sphere(name, position, size, color, bone=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=1, location=position)
    obj = bpy.context.object
    obj.scale = size
    for face in obj.data.polygons:
        face.use_smooth = True
    return finish(obj, name, color, bone)

def cylinder(name, position, radius, depth, color, bone=None, vertices=16, rotation=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=position)
    obj = bpy.context.object
    if rotation:
        obj.rotation_euler = rotation
    return finish(obj, name, color, bone)

def limb(name, start, end, width, depth, color, bone):
    start, end = Vector(start), Vector(end)
    obj = box(name, (start + end) * .5, (width, depth, (end - start).length + .035), color, width * .3, bone)
    obj.rotation_euler = (end - start).to_track_quat('Z', 'Y').to_euler()
    return obj

def loft(name, rings, color, *, segments=12, crease=0):
    """Connected elliptical rings; each ring carries explicit deform-bone weights."""
    count=segments
    vertices=[]
    faces=[]
    for i,(center,width,depth,weights) in enumerate(rings):
        c=Vector(center)
        before=Vector(rings[max(0,i-1)][0])
        after=Vector(rings[min(len(rings)-1,i+1)][0])
        tangent=(after-before).normalized()
        sideways=Vector((tangent.z,0,-tangent.x)).normalized()
        front=Vector((0,1,0))
        for j in range(count):
            angle=j*2*math.pi/count
            fold=crease*(.56*math.sin(angle*5+i*1.12)+.44*math.sin(angle*9-i*.77))
            vertices.append(c+sideways*math.cos(angle)*(width+fold)+front*math.sin(angle)*(depth+fold))
        if i:
            for j in range(count):
                a=(i-1)*count+j
                b=(i-1)*count+(j+1)%count
                faces.append((a,b,i*count+(j+1)%count,i*count+j))
    faces.append(tuple(reversed(range(count))))
    faces.append(tuple((len(rings)-1)*count+j for j in range(count)))
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    bm=bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(mesh)
    bm.free()
    obj=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    finish(obj,name,color)
    for i,(_,_,_,weights) in enumerate(rings):
        for bone,weight in weights.items():
            group=obj.vertex_groups.get(bone) or obj.vertex_groups.new(name=bone)
            group.add(list(range(i*count,(i+1)*count)),weight,'REPLACE')
    for polygon in mesh.polygons:
        polygon.use_smooth=True
    return obj

def jointed_limb(name,start,joint,end,width,depth,color,upper,lower):
    a,b,c=Vector(start),Vector(joint),Vector(end)
    rings=[]
    for t,w,d,weights in [
        (0,.88,.92,{upper:1}),(.25,1,1,{upper:1}),(.72,.88,.91,{upper:1}),
        (.9,.84,.85,{upper:.85,lower:.15}),
        (1,.84,.85,{upper:.5,lower:.5})]:
        rings.append((a.lerp(b,t),width*w*.5,depth*d*.5,weights))
    for t,w,d,weights in [
        (.1,.84,.85,{upper:.15,lower:.85}),(.28,.88,.91,{lower:1}),
        (.7,.79,.8,{lower:1}),(1,.72,.73,{lower:1})]:
        rings.append((b.lerp(c,t),width*w*.5,depth*d*.5,weights))
    return loft(name,rings,color)

def join(objects, name):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    # Join preserves material entries; consolidate to the single atlas slot.
    obj.data.materials.clear()
    obj.data.materials.append(ATLAS)
    for polygon in obj.data.polygons:
        polygon.material_index = 0
    return obj

def make_rig(name):
    data = bpy.data.armatures.new(name + '_Skeleton')
    rig = bpy.data.objects.new(name + '_Rig', data)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    specs = [
        ('Root', (0,0,0), (0,0,.2), None),
        ('Hips', (0,0,.94), (0,0,1.09), 'Root'),
        ('Spine', (0,0,1.09), (0,0,1.24), 'Hips'),
        ('Chest', (0,0,1.24), (0,0,1.48), 'Spine'),
        ('Neck', (0,0,1.48), (0,0,1.60), 'Chest'),
        ('Head', (0,0,1.60), (0,0,1.83), 'Neck')]
    for side, sign in (('Left', 1), ('Right', -1)):
        specs += [
            (side+'Shoulder', (sign*.1,0,1.43), (sign*.24,0,1.43), 'Chest'),
            (side+'UpperArm', (sign*.24,0,1.43), (sign*.40,0,1.13), side+'Shoulder'),
            (side+'LowerArm', (sign*.40,0,1.13), (sign*.53,0,.88), side+'UpperArm'),
            (side+'Hand', (sign*.53,0,.88), (sign*.57,0,.78), side+'LowerArm'),
            (side+'UpperLeg', (sign*.14,0,.94), (sign*.14,0,.53), 'Hips'),
            (side+'LowerLeg', (sign*.14,0,.53), (sign*.14,0,.14), side+'UpperLeg'),
            (side+'Foot', (sign*.14,0,.14), (sign*.14,-.18,.075), side+'LowerLeg'),
            (side+'Toes', (sign*.14,-.18,.075), (sign*.14,-.29,.075), side+'Foot')]
    for bone_name, head, tail, parent in specs:
        bone = data.edit_bones.new(bone_name)
        bone.head, bone.tail = head, tail
        if parent:
            bone.parent = data.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    rig.show_in_front = True
    MANIFEST['rig_bones'] = [s[0] for s in specs]
    return rig

def character(name,role,variant=0):
    global ATLAS
    sys.path.insert(0,str(Path(__file__).resolve().parent))
    if role == 'body':
        import body_character
        shared_atlas = ATLAS
        ATLAS = body_character.make_detail_atlas(sys.modules[__name__], shared_atlas)
        try:
            return body_character.build(sys.modules[__name__], name)
        finally:
            ATLAS = shared_atlas
    import character_realism
    return character_realism.build(sys.modules[__name__],name,role,variant)

def export_asset(name, objects, kind):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    path=EXPORT/'Models'/(name+'.fbx')
    body = name == 'Body_Blue'
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},
        global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
        use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=body,
        bake_anim_use_all_actions=body,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=.15,
        path_mode='RELATIVE',axis_forward='-Z',axis_up='Y',use_custom_props=True)
    triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects if o.type=='MESH')
    meshes=[o for o in objects if o.type=='MESH']
    audit={'unweighted_vertices':0,'invalid_weight_vertices':0,'unknown_groups':0,'blended_vertices':0,'max_influences':0,
           'uv_layers':sorted(set(layer.name for o in meshes for layer in o.data.uv_layers))}
    if kind=='character':
        bones=set(objects[0].data.bones.keys())
        for mesh in meshes:
            for vertex in mesh.data.vertices:
                raw=list(vertex.groups)
                if any(mesh.vertex_groups[g.group].name not in bones for g in raw): audit['unknown_groups']+=1
                invalid=any(not math.isfinite(g.weight) or g.weight<0 for g in raw)
                weights=[g.weight for g in raw if mesh.vertex_groups[g.group].name in bones and math.isfinite(g.weight) and g.weight>0]
                audit['max_influences']=max(audit['max_influences'],len(weights))
                if not weights: audit['unweighted_vertices']+=1
                if invalid or abs(sum(weights)-1)>.0001:
                    audit['invalid_weight_vertices']+=1
                if len(weights)>1: audit['blended_vertices']+=1
        assert audit['unweighted_vertices']==0 and audit['invalid_weight_vertices']==0 and audit['unknown_groups']==0, audit
        assert audit['max_influences']<=4, audit
    assert audit['uv_layers']==['PaletteUV'],audit
    evaluated_triangles=0
    depsgraph=bpy.context.evaluated_depsgraph_get()
    for obj in meshes:
        evaluated=obj.evaluated_get(depsgraph)
        evaluated_mesh=evaluated.to_mesh()
        evaluated_mesh.calc_loop_triangles()
        evaluated_triangles+=len(evaluated_mesh.loop_triangles)
        evaluated.to_mesh_clear()
    points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
    size=[round(max(p[i] for p in points)-min(p[i] for p in points),3) for i in range(3)]
    MANIFEST['assets'].append({'name':name,'kind':kind,'file':str(path.relative_to(PROJECT)),
        'triangles':triangles,'evaluated_triangles':evaluated_triangles,'materials':1,
        'size_blender_xyz_m':size,'rigged':kind=='character','audit':audit})
    if body:
        MANIFEST['assets'][-1]['bone_count'] = len(objects[0].data.bones)
        MANIFEST['assets'][-1]['animations'] = sorted(action.name for action in bpy.data.actions if action.name.startswith('Body_'))
        MANIFEST['assets'][-1]['texture_size'] = [2048, 2048]

def prop(name, build):
    before=set(bpy.data.objects)
    build()
    mesh=join([o for o in set(bpy.data.objects)-before if o.type=='MESH'],name+'_Mesh')
    export_asset(name,[mesh],'prop')
    return mesh

def setup_render():
    scene=bpy.context.scene
    scene.render.engine='CYCLES'
    scene.cycles.samples=32
    scene.cycles.use_denoising=True
    scene.render.resolution_x=1800
    scene.render.resolution_y=1100
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    if scene.world is None: scene.world=bpy.data.worlds.new('Hide iT studio world')
    scene.world.color=(.28,.28,.28)
    scene.view_settings.view_transform='AgX'
    scene.render.film_transparent=False
    for name,position,energy,size,color in [
        ('Key',(2,-6,8),1800,7,(1,.88,.73)),('Fill',(-6,-2,5),1100,6,(.65,.83,1)),
        ('Rim',(2,5,7),1700,5,(.64,1,.92))]:
        data=bpy.data.lights.new(name,'AREA')
        data.energy=energy
        data.shape='DISK'
        data.size=size
        data.color=color
        obj=bpy.data.objects.new(name,data)
        bpy.context.collection.objects.link(obj)
        obj.location=position
        obj.rotation_euler=(Vector((0,0,1))-obj.location).to_track_quat('-Z','Y').to_euler()
    data=bpy.data.cameras.new('StudyCamera')
    camera=bpy.data.objects.new('StudyCamera',data)
    bpy.context.collection.objects.link(camera)
    camera.location=(6,-13,7)
    camera.rotation_euler=(Vector((0,0,1))-camera.location).to_track_quat('-Z','Y').to_euler()
    data.type='ORTHO'
    data.ortho_scale=11
    scene.camera=camera
    return camera

def label(text,position,size=.18,color='cream'):
    curve=bpy.data.curves.new(text,'FONT')
    curve.body=text
    curve.align_x='CENTER'
    curve.size=size
    curve.extrude=.001
    obj=bpy.data.objects.new(text,curve)
    bpy.context.collection.objects.link(obj)
    obj.location=position
    obj.rotation_euler=(math.pi/2,0,0)
    material=bpy.data.materials.get('Label_'+color)
    if material is None:
        material=bpy.data.materials.new('Label_'+color)
        material.diffuse_color=rgba(color)
    curve.materials.append(material)
    return obj

def world_bone_rotation(rig,bone_name,axis,angle):
    bone=rig.pose.bones[bone_name]
    basis=bone.bone.matrix_local.to_quaternion()
    bone.rotation_mode='QUATERNION'
    bone.rotation_quaternion=basis.inverted()@Quaternion(axis,math.radians(angle))@basis

def render_character_views(characters,camera):
    scene=bpy.context.scene
    scene.render.filepath=str(PREVIEW/'01-character-lineup.png')
    bpy.ops.render.render(write_still=True)
    saved_location=camera.location.copy()
    saved_rotation=camera.rotation_euler.copy()
    saved_scale=camera.data.ortho_scale
    first=characters[0][0]
    camera.location=(first.location.x+.5,-2,1.82)
    camera.rotation_euler=(Vector((first.location.x,0,1.67))-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.ortho_scale=.62
    scene.render.resolution_x=900
    scene.render.resolution_y=900
    scene.render.filepath=str(PREVIEW/'08-face-study.png')
    bpy.ops.render.render(write_still=True)
    camera.location=saved_location
    camera.rotation_euler=saved_rotation
    camera.data.ortho_scale=saved_scale
    scene.render.resolution_x=1800
    scene.render.resolution_y=1100
    # Show role prototypes separately from the five selectable ordinary people.
    role_characters=[characters[0]]+characters[5:8]
    for rig,objects,role in characters:
        for obj in objects: obj.hide_render=True
    for i,(rig,objects,role) in enumerate(role_characters):
        rig.location.x=(i-1.5)*2
        rig.location.y=0
        for obj in objects: obj.hide_render=False
    for obj in bpy.data.objects:
        if obj.type=='FONT' and obj.name!='Hide iT' and not obj.name.startswith('ZanettiZONEai'): obj.hide_render=True
        if obj.name.startswith('Display plinth'): obj.hide_render=True
    for i,(rig,objects,role) in enumerate(role_characters):
        cylinder('Role plinth',(rig.location.x,0,-.10),.78,.20,'floor',vertices=48)
        label(role.upper(),(rig.location.x,-.79,-.045),.16)
    for rig,objects,role in role_characters:
        if role=='body':
            box('Pose bench',(rig.location.x,0,.125),(.8,2,.25),'floor',.04)
            rig.rotation_euler=(-math.pi/2,0,0)
            rig.location.y=-.85
            rig.location.z=.4
        else:
            for side in ('Left','Right'):
                world_bone_rotation(rig,side+'UpperArm',(1,0,0),-60)
                world_bone_rotation(rig,side+'LowerArm',(1,0,0),-28)
                world_bone_rotation(rig,side+'UpperLeg',(1,0,0),-12)
                world_bone_rotation(rig,side+'LowerLeg',(1,0,0),30)
            world_bone_rotation(rig,'Chest',(1,0,0),12)
    bpy.context.view_layer.update()
    scene.render.filepath=str(PREVIEW/'02-carry-pose-study.png')
    bpy.ops.render.render(write_still=True)

def render_civilian_views(characters,camera):
    scene=bpy.context.scene
    scene.view_layers[0].material_override=None
    scene.world.use_nodes=False
    scene.world.color=(.28,.28,.28)
    for obj in bpy.data.objects:
        if obj.type=='FONT' or obj.name.startswith(('Display plinth','Role plinth','Pose bench')): obj.hide_render=True
        if obj.name=='Studio floor': obj.hide_render=False
    for rig,objects,role in characters:
        for obj in objects: obj.hide_render=True
    sys.path.insert(0,str(Path(__file__).resolve().parent))
    import character_realism
    for i,(rig,objects,role) in enumerate(characters[8:]):
        rig.location=((i-2.5)*1.8,0,0)
        rig.rotation_euler=(0,0,-.10)
        for obj in objects: obj.hide_render=False
        cylinder('Civilian plinth',(rig.location.x,0,-.1),.70,.2,'floor',vertices=48)
        label(character_realism.CITIZENS[i][1].upper(),(rig.location.x,-.72,-.045),.16)
    label('Hide iT / ORDINARY PASSERS-BY',(0,.5,2.55),.35,'mint')
    camera.location=(5,-15,6)
    camera.rotation_euler=(Vector((0,0,1))-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.ortho_scale=13
    scene.render.resolution_y=1100
    scene.render.filepath=str(PREVIEW/'07-civilian-lineup.png')
    bpy.ops.render.render(write_still=True)
    # Restore a neutral pose for a real silhouette check.
    for rig,objects,role in characters:
        rig.rotation_euler=(0,0,0)
        rig.location.y=0
        rig.location.z=0
        for bone in rig.pose.bones:
            bone.rotation_mode='QUATERNION'
            bone.rotation_quaternion=(1,0,0,0)
    for obj in bpy.data.objects:
        if obj.name.startswith(('Display plinth','Role plinth','Pose bench')) or obj.name=='Studio floor': obj.hide_render=True
    silhouette=bpy.data.materials.new('Review silhouette')
    silhouette.use_nodes=True
    nodes=silhouette.node_tree.nodes
    nodes.clear()
    emission=nodes.new('ShaderNodeEmission')
    emission.inputs['Color'].default_value=(0,0,0,1)
    out=nodes.new('ShaderNodeOutputMaterial')
    silhouette.node_tree.links.new(emission.outputs[0],out.inputs['Surface'])
    scene.view_layers[0].material_override=silhouette
    scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value=(1,1,1,1)
    camera.location=(0,-15,2.0)
    camera.rotation_euler=(Vector((0,0,1.3))-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.ortho_scale=9.5
    scene.render.resolution_y=900
    scene.render.filepath=str(PREVIEW/'03-silhouette-check.png')
    bpy.ops.render.render(write_still=True)

def main():
    global ATLAS
    reset()
    bpy.context.scene.unit_settings.system='METRIC'
    bpy.context.scene.unit_settings.scale_length=1
    ATLAS=make_palette()
    sys.path.insert(0,str(Path(__file__).resolve().parent))
    import character_realism
    MANIFEST['selectable_players']=[{'id':row[0],'name':row[1]} for row in character_realism.OUTFITS]
    MANIFEST['ambient_civilians']=[{'id':row[0],'name':row[1]} for row in character_realism.CITIZENS]
    characters=[]
    specs=[(row[0],'player',i) for i,row in enumerate(character_realism.OUTFITS)]
    specs += [('Security_Navy','guard',0),('Guest_Plum','civilian',0),('Body_Blue','body',0)]
    specs += [(row[0],'civilian',i+1) for i,row in enumerate(character_realism.CITIZENS)]
    obsolete=EXPORT/'Models'/'Player_Orange.fbx'
    if obsolete.exists(): obsolete.unlink()
    for name,role,variant in specs:
        rig,objects=character(name,role,variant)
        export_asset(name,objects,'character')
        characters.append((rig,objects,role))
    for i,(rig,objects,role) in enumerate(characters):
        rig.location.x=(i-2)*2.0 if i<5 else (i-6)*2
        rig.location.y=0 if i<5 else 4
        rig.rotation_euler[2]=-.10
        rig.pose.bones['LeftUpperArm'].rotation_mode='XYZ'
        rig.pose.bones['RightUpperArm'].rotation_mode='XYZ'
        # Neutral A-pose is retained for import and pose inspection.
        if i<5:
            cylinder('Display plinth',(rig.location.x,0,-.10),.78,.20,'floor',vertices=48)
            label(character_realism.OUTFITS[i][1].upper(),(rig.location.x,-.79,-.045),.16)
        else:
            for obj in objects: obj.hide_render=True
    box('Studio floor',(0,0,-.26),(200,200,.2),'ink',0)
    camera=setup_render()
    camera.data.ortho_scale=12.5
    label('Hide iT',(0,.5,3.0),.48,'mint')
    label('ZanettiZONEai  /  SELECTABLE ADULT OUTFIT STUDY',(0,.5,2.65),.16)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'HIdeIT_CharacterStudy.blend'))
    bpy.ops.file.make_paths_relative()
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'HIdeIT_CharacterStudy.blend'))
    render_character_views(characters,camera)
    render_civilian_views(characters,camera)
    import world_kit
    world_kit.build(sys.modules[__name__])
    (ART/'asset-manifest.json').write_text(json.dumps(MANIFEST,indent=2),encoding='utf-8')
    print('HIDEIT_ASSET_BUILD_OK='+str(ART/'asset-manifest.json'))

def build_body_only():
    global ATLAS
    reset()
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1
    ATLAS = make_palette()
    rig, objects = character('Body_Blue', 'body')
    export_asset('Body_Blue', objects, 'character')
    manifest_path = ART / 'asset-manifest.json'
    manifest = json.loads(manifest_path.read_text(encoding='utf-8')) if manifest_path.exists() else MANIFEST
    new_asset = MANIFEST['assets'][0]
    manifest['assets'] = [asset for asset in manifest['assets'] if asset['name'] != 'Body_Blue'] + [new_asset]
    manifest['palette'] = PALETTE
    manifest_path.write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')

    box('Studio floor', (0, 0, -.10), (200, 200, .20), 'ink', 0)
    camera = setup_render()
    scene.cycles.samples = 20
    scene.render.resolution_x = 1400
    scene.render.resolution_y = 1600
    scene.render.fps = 30
    scene.frame_end = 52
    camera.data.ortho_scale = 2.15
    camera.location = (1.25, -5, 1.40)
    camera.rotation_euler = (Vector((0, 0, .915)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'Hide-iT-Body.blend'))
    if '--skip-renders' in sys.argv:
        return
    world_bone_rotation(rig, 'LeftUpperArm', (0,1,0), 16)
    world_bone_rotation(rig, 'RightUpperArm', (0,1,0), -16)
    scene.render.filepath = str(PREVIEW / 'body-01-model.png')
    bpy.ops.render.render(write_still=True)

    scene.render.resolution_x = 1440
    scene.render.resolution_y = 1440
    camera.data.ortho_scale = .38
    camera.location = (.45, -2.2, 1.76)
    camera.rotation_euler = (Vector((0, 0, 1.65)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.filepath = str(PREVIEW / 'body-02-face.png')
    bpy.ops.render.render(write_still=True)

    rig.animation_data.action = bpy.data.actions['Body_Collapse']
    scene.frame_set(52)
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    camera.data.ortho_scale = 2.50
    camera.location = (2.32, -1.53, 3.45)
    camera.rotation_euler = (Vector((0, 0, .13)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.filepath = str(PREVIEW / 'body-03-resting-pose.png')
    bpy.ops.render.render(write_still=True)

if __name__=='__main__':
    if '--body-only' in sys.argv:
        build_body_only()
    else:
        main()
