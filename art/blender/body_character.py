"""Original civilian ragdoll for Hide iT, modeled in meters on the shared rig."""

import math

import bmesh
import bpy
from mathutils import Vector

from body_head import FABRIC_TILES, add_head, make_detail_atlas
from body_animation import add_finger_bones, create_actions


def surface(api, name, rows, color, weights, *, closed=False, capped=False):
    width = len(rows[0])
    faces = []
    for row in range(len(rows) - 1):
        for column in range(width if closed else width - 1):
            next_column = (column + 1) % width
            base = row * width
            faces.append((base + column, base + next_column,
                          base + width + next_column, base + width + column))
    if capped:
        faces.extend((tuple(reversed(range(width))),
                      tuple((len(rows) - 1) * width + c for c in range(width))))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([point for row in rows for point in row], [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.select_set(True)
    api.finish(obj, name, color)
    for row, influences in enumerate(weights):
        for bone, weight in influences.items():
            group = obj.vertex_groups.get(bone) or obj.vertex_groups.new(name=bone)
            group.add(list(range(row * width, (row + 1) * width)), weight, 'REPLACE')
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    return obj


def cord(api, name, points, radii, color, bone, sides=6):
    """A tapered tube for cloth edging, small face contours, and hair locks."""
    points = [Vector(p) for p in points]
    rows = []
    for i, (point, radius) in enumerate(zip(points, radii)):
        tangent = (points[min(i + 1, len(points) - 1)] - points[max(i - 1, 0)]).normalized()
        hint = Vector((0, 1, 0)) if abs(tangent.y) < .92 else Vector((1, 0, 0))
        u = tangent.cross(hint).normalized()
        v = tangent.cross(u)
        rows.append([point + radius * (u * math.cos(a) + v * math.sin(a))
                     for a in (j * math.tau / sides for j in range(sides))])
    return surface(api, name, rows, color, [{bone: 1}] * len(rows), closed=True, capped=True)


TORSO = [
    (.94, .199, .126, -.00, {'Hips': 1}),
    (.99, .211, .144, -.010, {'Hips': .65, 'Spine': .35}),
    (1.05, .202, .145, -.012, {'Hips': .25, 'Spine': .75}),
    (1.15, .213, .153, -.007, {'Spine': .65, 'Chest': .35}),
    (1.25, .239, .162, -.002, {'Spine': .20, 'Chest': .80}),
    (1.34, .259, .163, .003, {'Chest': 1}),
    (1.41, .258, .145, .008, {'Chest': 1}),
    (1.45, .218, .126, .013, {'Chest': 1}),
    (1.49, .112, .094, .013, {'Chest': .9, 'Neck': .1}),
]


def outfit(api):
    jacket = []
    shirt = []
    for row, (z, x_rad, y_rad, y, _) in enumerate(TORSO):
        opening = .50 + .045 * math.sin(row * .8)
        jacket_ring = []
        for i in range(57):
            a = opening + (math.tau - 2 * opening) * i / 56
            fold = .005 * math.cos(7 * a + row * .8) + .005 * math.cos(14 * a - row * .58)
            jacket_ring.append(((x_rad + fold) * math.sin(a), y - (y_rad + fold) * math.cos(a), z))
        jacket.append(jacket_ring)
        shirt.append([
            ((x_rad - .016) * math.sin(a), y - (y_rad - .020) * math.cos(a), z - .003)
            for a in (math.tau * i / 24 for i in range(24))
        ])
    weights = [r[-1] for r in TORSO]
    surface(api, 'Heather crew-neck shirt', shirt, 'body_jersey', weights, closed=True, capped=True)
    surface(api, 'Open charcoal fleece jacket', jacket, 'body_fleece', weights)
    for side in (0, -1):
        points = [Vector(row[side]) + Vector((0, -.004, 0)) for row in jacket]
        # Zip tape and piping follow the sampled fabric edges, with the same skin weights.
        rows = [[p + Vector((x, y, 0)) for x, y in
                 ((.004, 0), (.003, .003), (0, .004), (-.003, .003),
                  (-.004, 0), (-.003, -.003), (0, -.004), (.003, -.003))] for p in points]
        surface(api, 'Zipper piping', rows, 'body_fleece_edge', weights, closed=True, capped=True)
    cord(api, 'Rounded shirt neckline', [(-.080,-.057,1.486),(-.064,-.084,1.469),
             (0,-.090,1.460),(.064,-.084,1.469),(.080,-.057,1.486)], [.004]*5,
         'body_jersey_trim', 'Chest')
    for sign in (-1, 1):
        cord(api, 'Hood drawstring', [(sign*.104,-.073,1.472),(sign*.121,-.122,1.421),
             (sign*.114,-.154,1.385),(sign*.097,-.154,1.332)],
             [.0027,.0024,.0022,.0021], 'body_fleece_edge', 'Chest', 8)
    cord(api, 'Zipper pull', [(.098,-.139,1.044),(.105,-.148,1.026),(.108,-.146,1.017)],
         [.004,.004,.003], 'brass', 'Spine')
    api.loft('Fleece hood resting on the back', [
        ((0,.067,1.344), .052,.034,{'Chest':1}),
        ((0,.095,1.374), .114,.058,{'Chest':1}),
        ((0,.116,1.426), .150,.068,{'Chest':1}),
        ((0,.104,1.476), .145,.060,{'Chest':1}),
        ((0,.071,1.504), .104,.042,{'Chest':1}),
    ], 'body_fleece')
    cord(api, 'Hood hem', [(-.119,.097,1.465),(-.111,.150,1.440),
         (-.064,.172,1.394),(0,.174,1.375),(.064,.172,1.394),
         (.111,.150,1.440),(.119,.097,1.465)], [.0035]*7, 'body_fleece_edge', 'Chest')
    for sign in (-1, 1):
        cord(api, 'Pocket welt', [(sign*x,y,z) for x,y,z in
             ((.116,-.138,1.035),(.135,-.135,1.062),(.157,-.124,1.097),(.174,-.110,1.118))],
             [.003,.004,.004,.0025], 'body_fleece_edge', 'Spine')
        cord(api, 'Back pocket stitch', [(sign*x,y,z) for x,y,z in
             ((.039,.130,.928),(.053,.133,.877),(.103,.125,.851),
              (.151,.104,.875),(.162,.092,.928))], [.0015]*5, 'body_denim_seam', 'Hips')


def sneaker(api, side, sign):
    foot, toes = side + 'Foot', side + 'Toes'
    sections = [(.087,.060,.111),(.055,.072,.140),(-.008,.075,.144),
                (-.081,.081,.104),(-.160,.082,.080),(-.218,.074,.065),(-.240,.050,.052)]
    center = sign * .14
    profile = [(-.91,0),(-1,.21),(-.90,.62),(-.53,.94),(0,1),
               (.53,.94),(.90,.62),(1,.21),(.91,0),(0,-.03)]
    for is_sole in (False, True):
        rows, weights = [], []
        for y, radius, height in sections:
            bottom = .018 if is_sole else .037
            rise = .024 if is_sole else height - bottom
            rows.append([(center + radius * x, y, bottom + rise * z) for x, z in reversed(profile)])
            blend = .65 if y < -.18 else .33 if y < -.12 else 0
            weights.append({foot: 1 - blend, toes: blend} if blend else {foot:1})
        surface(api, 'Rubber outsole' if is_sole else 'Canvas sneaker', rows,
                'body_sole' if is_sole else 'body_canvas', weights, closed=True, capped=True)
    for i in range(4):
        y = -.032 - i * .022
        z = .136 - i * .013
        cord(api, 'Flat sneaker lace', [(center-.028,y,z-.006),(center,y-.009,z),
             (center+.028,y-.004,z-.006)], [.002]*3, 'cream', foot)


def fingers(api, side, sign):
    hand = side + 'Hand'
    api.loft('Hand and knuckles', [
        ((sign*.538,0,.874), .023,.022, {hand:1}),
        ((sign*.548,-.005,.846), .034,.024, {hand:1}),
        ((sign*.557,-.006,.821), .039,.023, {hand:1}),
        ((sign*.561,-.011,.802), .033,.019, {hand:1}),
    ], 'skin_body', segments=24)
    skeleton = []
    for index, digit in enumerate(('Index','Middle','Ring','Pinky','Thumb')):
        x = .532 + index * .018
        long = (.067, .076, .069, .055, .049)[index]
        start = Vector((sign*x,-.008,.817 + (.002 if index==0 else 0)))
        offset = Vector((sign*(index-1.2)*.002,-.018,-long))
        radius = .0085 if index < 3 else .0072
        if digit == 'Thumb':
            start = Vector((sign*.534,-.013,.848))
            offset = Vector((-sign*.025,-.026,-long))
            radius = .012
        points = [start + offset*t + Vector((0,.009*math.sin(math.pi*t),0))
                  for t in (0,.38,.72,1)]
        names = [f'{side}{digit}{n}' for n in range(1,4)]
        parent = hand
        rings = [(tuple(points[0]), radius, radius * .86, {hand:.55, names[0]:.45})]
        for n, name in enumerate(names):
            skeleton.append((name, points[n], points[n+1], parent))
            a, b = points[n:n+2]
            width = radius * (1-.14*n)
            rings.append((tuple(a.lerp(b,.38)), width, width*.87, {name:1}))
            weights = {name:.6, names[n+1]:.4} if n < 2 else {name:1}
            rings.append((tuple(b), width*(.90 if n<2 else .62), width*.70, weights))
            parent = name
        api.loft(digit, rings, 'skin_body', segments=14)
        tip = points[2].lerp(points[3],.58) + Vector((0,radius*.63,0))
        api.sphere('Short fingernail', tip, (radius*.62,.0026,.007), 'body_nail', names[2])
    return skeleton


def jeans(api):
    halves = []
    count = 36
    for side, sign in (('Left', 1), ('Right', -1)):
        thigh, shin = side+'UpperLeg', side+'LowerLeg'
        angles = [i*math.tau/count for i in range(count)]
        # Both waist halves meet along the crotch curve. They share the center vertices after welding.
        rows = [[(sign*max(0,.194*math.cos(a)), -.119*math.sin(a), .974) for a in angles],
                [(sign*max(0,.217*math.cos(a)), -.129*math.sin(a)+.005,
                  .889+.041*max(0,math.cos(a))-.062*max(0,-math.cos(a))) for a in angles]]
        weights = [{'Hips':1}, {'Hips':1}]
        rings = [(.137,.008,.817,.119,.122,{thigh:.75,'Hips':.25}),
                 (.148,.000,.728,.109,.110,{thigh:1}),
                 (.143,-.004,.581,.093,.098,{thigh:.84,shin:.16}),
                 (.141,-.006,.530,.089,.099,{thigh:.5,shin:.5}),
                 (.142,.000,.474,.093,.097,{thigh:.16,shin:.84}),
                 (.144,.009,.324,.083,.088,{shin:1}),
                 (.144,.012,.214,.087,.099,{shin:1}),
                 (.144,.004,.184,.088,.090,{shin:1}),
                 (.144,.004,.165,.084,.091,{shin:1}),
                 (.144,.001,.142,.080,.084,{shin:1})]
        for row, (x,y,z,w,d,influences) in enumerate(rings):
            ring = []
            for a in angles:
                fold = .004*math.cos(5*a+row*1.1)+.003*math.sin(9*a-row*.8)
                ring.append((sign*(x+(w+fold)*math.cos(a)), y-(d+fold)*math.sin(a),
                             z+.009*math.sin(3*a+row*.71)))
            rows.append(ring)
            weights.append(influences)
        if sign < 0:
            rows = [list(reversed(row)) for row in rows]
        # Rings run from waist to ankle. Reverse the row order for outward normals.
        halves.append(surface(api, 'Denim leg and hip', rows[::-1], 'body_denim', weights[::-1], closed=True))
        cord(api, 'Denim running stitch', [(sign*(x+w+.002),y,z) for x,y,z,w,_,_ in rings[:2]],
             [.0013,.0013], 'body_denim_seam', thigh)
    pants = api.join(halves, 'Continuous denim trousers')
    bm = bmesh.new()
    bm.from_mesh(pants.data)
    internal = [face for face in bm.faces if all(abs(vertex.co.x) < .00001 for vertex in face.verts)]
    bmesh.ops.delete(bm, geom=internal, context='FACES_ONLY')
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=.00003)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(pants.data)
    bm.free()


def limbs(api):
    digits = []
    for side, sign in (('Left', 1), ('Right', -1)):
        upper, forearm, hand = (side + bone for bone in ('UpperArm', 'LowerArm', 'Hand'))
        shoulder = side + 'Shoulder'
        api.loft('Tapered fleece sleeve', [
            ((sign*.160,.010,1.405),.035,.068,{shoulder:.88,upper:.12}),
            ((sign*.205,.010,1.420),.058,.087,{shoulder:.61,upper:.39}),
            ((sign*.240,.008,1.396),.083,.098,{shoulder:.45,upper:.55}),
            ((sign*.280,.005,1.349),.091,.104,{upper:1}),
            ((sign*.343,.002,1.247),.085,.092,{upper:1}),
            ((sign*.383,.006,1.164),.084,.089,{upper:.84,forearm:.16}),
            ((sign*.401,.006,1.130),.080,.087,{upper:.48,forearm:.52}),
            ((sign*.416,.009,1.095),.086,.091,{upper:.15,forearm:.85}),
            ((sign*.456,.003,1.024),.078,.084,{forearm:1}),
            ((sign*.502,.004,.938),.066,.070,{forearm:1}),
            ((sign*.518,.000,.902),.059,.061,{forearm:.93,hand:.07}),
        ], 'body_fleece', segments=32, crease=.006)
        api.loft('Ribbed wrist cuff', [
            ((sign*.504,0,.929),.070,.072,{forearm:1}),
            ((sign*.532,0,.881),.055,.052,{forearm:.84,hand:.16}),
            ((sign*.543,0,.863),.045,.042,{forearm:.65,hand:.35})], 'body_ribbing', segments=32)
        digits.extend(fingers(api, side, sign))
        sneaker(api, side, sign)
    jeans(api)
    return digits


def build(api, name):
    existing = set(bpy.data.objects)
    outfit(api)
    digits = limbs(api)
    add_head(api, surface, cord)
    objects = sorted((obj for obj in set(bpy.data.objects)-existing if obj.type=='MESH'), key=lambda obj: obj.name)
    mesh = api.join(objects, name+'_Mesh')
    map_fabric_detail(api, mesh.data)
    rig = api.make_rig(name)
    add_finger_bones(rig, digits)
    modifier = mesh.modifiers.new('Ragdoll skin','ARMATURE')
    modifier.object = rig
    mesh.parent = rig
    markers = []
    for marker_name, location in [
        ('FaceForward',(0,-.40,1.69)), ('GripHead',(0,.020,1.838)),
        ('GripLeftHand',(.548,-.007,.820)), ('GripRightHand',(-.548,-.007,.820)),
        ('GripLeftAnkle',(.144,0,.159)), ('GripRightAnkle',(-.144,0,.159)),
    ]:
        marker = bpy.data.objects.new(f'{name}__{marker_name}', None)
        bpy.context.collection.objects.link(marker)
        marker.parent = rig
        marker.location = location
        markers.append(marker)
    rig['role'] = 'body'
    rig['description'] = 'Charcoal hoodie, stone jersey, washed denim and canvas shoes. MakeHuman CC0 face base.'
    create_actions(rig)
    return rig, [rig,mesh,*markers]


def map_fabric_detail(api, mesh):
    """Stay inside each palette cell while giving its pattern room on the mesh."""
    uv = mesh.uv_layers.active.data
    for loop in mesh.loops:
        corner = uv[loop.index]
        if corner.uv.x > 1:
            corner.uv.x -= 2
            continue
        column, row = int(corner.uv.x * 8), int(corner.uv.y * 8)
        color = api.COLORS[row * 8 + column]
        x, y, z = mesh.vertices[loop.vertex_index].co
        if 'denim' in color:
            bottom, height = .13, .87
        elif color in ('skin_body','body_lids','body_lips','body_hair','body_hair_light','body_beard'):
            bottom, height = 1.45, .42
        elif color in ('body_canvas','body_sole','cream'):
            bottom, height = 0, .18
        else:
            bottom, height = .72, .80
        u = max(.10, min(.90, .5 + x * .60 + y * .74))
        v = .10 + .80 * max(0, min(1, (z - bottom) / height))
        if color in ('body_denim', 'body_fleece', 'body_jersey', 'body_canvas'):
            tile_x, tile_y = FABRIC_TILES[color]
            if color == 'body_denim':
                u = .16 + .68 * (math.atan2(abs(x) - .145, -y) / math.tau + .5)
            corner.uv = ((tile_x + u) / 4, (tile_y + v) / 4)
        else:
            corner.uv = ((column + u) / 16, (row + v) / 16)
