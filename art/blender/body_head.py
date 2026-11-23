"""Adapt the MakeHuman CC0 face and generate the body-specific texture atlas."""

import math
from random import Random

import bpy
import numpy as np
from mathutils import Vector


FABRIC_TILES = {'body_denim': (2, 0), 'body_canvas': (3, 0),
                'body_fleece': (2, 1), 'body_jersey': (3, 1)}


def smooth(a, b, value):
    t = np.clip((value - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def face_mesh(api):
    source = api.ART / 'third-party' / 'makehuman'
    points, faces = [], []
    is_body = False
    for line in (source / 'base.obj').read_text(encoding='utf-8').splitlines():
        if line.startswith('v '):
            points.append(tuple(map(float, line.split()[1:4])))
        elif line.startswith('g '):
            is_body = line.strip() == 'g body'
        elif is_body and line.startswith('f '):
            faces.append(tuple(int(token.split('/')[0])-1 for token in line.split()[1:]))
    for line in (source / 'caucasian-male-young.target').read_text(encoding='utf-8').splitlines():
        data = line.split()
        if data and data[0].isdigit():
            index = int(data[0])
            points[index] = tuple(a + float(b) for a, b in zip(points[index], data[1:]))

    # Keep the scalp, ears, face and collar transition. The clothing obscures the lower cut.
    faces = [face for face in faces if all(points[i][1] > 6.40 and abs(points[i][0]) < 1.31 for i in face)]
    indices = {old: new for new, old in enumerate(sorted({i for face in faces for i in face}))}
    vertices = []
    for index in indices:
        x, h, depth = points[index]
        # Compress the actual lid topology toward a closed seam and preserve the eye socket.
        eye = abs(x)
        center = 8.171 + (eye - .28) * .12
        if .155 < eye < .515 and depth > 1.29 and abs(h-center) < .23:
            influence = (smooth(.155,.235,eye) * (1-smooth(.422,.515,eye)) *
                         smooth(1.29,1.45,depth) * (1-smooth(.058,.23,abs(h-center))))
            h += (center-h) * influence * .96
        z = 1.481+(h-6.60)*.116
        collar = .67+.33*float(smooth(1.463,1.53,z))
        vertices.append((x*.112*collar, .034-(depth-.36)*.104*collar, z))
    mesh = bpy.data.meshes.new('Anatomical face, ears, and neck')
    mesh.from_pydata(vertices, [], [tuple(indices[i] for i in face) for face in faces])
    mesh.update()
    obj = bpy.data.objects.new(mesh.name, mesh)
    bpy.context.collection.objects.link(obj)
    api.finish(obj, mesh.name, 'skin_body')
    groups = {name: obj.vertex_groups.new(name=name) for name in ('Chest','Neck','Head')}
    for index, (_, _, z) in enumerate(vertices):
        chest = float(1-smooth(1.464,1.519,z))
        head = float(smooth(1.535,1.601,z))
        for name, weight in (('Chest',chest),('Neck',1-chest-head),('Head',head)):
            if weight > 0:
                groups[name].add([index], weight, 'REPLACE')

    uvs = mesh.uv_layers.active.data
    for polygon in mesh.polygons:
        polygon.use_smooth = True
        corners = [vertices[mesh.loops[loop].vertex_index] for loop in polygon.loop_indices]
        angles = [math.atan2(x,.026-y) / math.tau + .5 for x,y,z in corners]
        if max(angles) - min(angles) > .5:
            if sum(x for x,y,z in corners) >= 0:
                angles = [angle if angle > .5 else 1 for angle in angles]
            else:
                angles = [angle if angle < .5 else 0 for angle in angles]
        for loop, angle, (_, _, z) in zip(polygon.loop_indices, angles, corners):
            # The temporary +2 offset identifies the face UVs through the object join.
            uvs[loop].uv = (2.004+.992*angle, .514+.473*float(np.clip((z-1.455)/.353,0,1)))


def add_head(api, surface, cord):
    face_mesh(api)

    def scalp(theta, phi, loft=0):
        x = (.101+loft)*math.sin(theta)*math.sin(phi)
        y = .025-(.109+loft)*math.sin(theta)*math.cos(phi)
        z = 1.724+(.103+loft)*math.cos(theta)
        return (x,y,z)

    def hairline(phi):
        return 1.38 + .64 * ((1-math.cos(phi))*.5)**.61 + .042*math.sin(phi*4+.8)

    phis = [math.tau*i/72 for i in range(72)]
    rings = [[scalp(.022+t*(hairline(a)-.022), a, .0017*math.sin(a*15+t*5)) for a in phis]
             for t in (0,.14,.25,.36,.47,.57,.65,.72,.78,.84,.89,.94,1)]
    surface(api, 'Wavy hair mass', rings, 'body_hair', [{'Head':1}]*len(rings), closed=True, capped=True)

    rng = Random(114)
    # Offset layers and give each lock a different path, depth and endpoint.
    for band, count in enumerate((13,20,26,34)):
        for i in range(count):
            phi = math.tau*(i+band*.37+rng.uniform(-.28,.28))/count
            start = .14+band*.29+rng.uniform(-.12,.09)
            end = min(hairline(phi)+rng.uniform(-.03,.12),start+rng.uniform(.48,.97))
            roll = rng.uniform(.26,.78)
            wave = rng.uniform(.06,.14)
            points = []
            for n in range(17):
                t = n/16
                turn = phi+roll*t+wave*math.sin(t*math.tau+i*.39)*math.sin(math.pi*t)
                elevation = start+(end-start)*t+.055*math.sin(2*math.pi*t)
                loft = .0015+.004*math.sin(math.pi*t)
                points.append(Vector(scalp(elevation,turn,loft)))
            radius = rng.uniform(.0046,.0095)
            sides = 10
            rows = []
            for n, point in enumerate(points):
                tangent = points[min(n+1,16)]-points[max(0,n-1)]
                outward = Vector((point.x, point.y-.025, point.z-1.724)).normalized()
                cross = outward.cross(tangent).normalized()
                normal = tangent.normalized().cross(cross).normalized()
                width = radius*(.10+.90*math.sin(math.pi*n/16)**.65)
                rows.append([point+width*(cross*math.cos(b)+.42*normal*math.sin(b))
                             for b in (j*math.tau/sides for j in range(sides))])
            color = 'body_hair' if i%5 else 'body_hair_light'
            lock = surface(api, 'Swept scalp lock', rows, color, [{'Head':1}]*len(rows), closed=True, capped=True)
            cell = api.COLORS.index(color)
            for loop, uv in zip(lock.data.loops,lock.data.uv_layers.active.data):
                row, corner = divmod(loop.vertex_index,sides)
                u = (cell%8+.20+.60*(.5+.5*math.cos(corner*math.tau/sides)))/16
                v = (cell//8+.18+.64*row/16)/16
                uv.uv = (2+u,v)


def tile_data(color, shades, u, v, random):
    threads = np.cos(math.tau*(63*u+77*v)) * .6 + np.sin(math.tau*(39*u-51*v)) * .4
    fleck = random.uniform(-1,1,(len(v),u.shape[1])).astype(np.float32)
    folds = np.sin((7*v+.32*np.sin(8*u))*math.tau) * np.sin(math.pi*u)**2
    gloss = .20
    if color == 'body_denim':
        thigh = np.exp(-((v-.59)/.19)**2) * np.exp(-((u-.52)/.23)**2)
        knee = np.exp(-((v-.42)/.075)**2)
        whisker = np.cos((20*v+.75*np.sin(u*14))*math.tau)*np.exp(-((v-.69)/.125)**2)
        delta = .19*thigh+.075*knee+.062*whisker+.046*threads+.035*fleck+.065*folds
        height = .12*threads+.13*folds+.07*fleck
        gloss = .24
    elif color in ('body_fleece','body_jersey','body_canvas','body_ribbing','body_fleece_edge'):
        knit = np.cos(u*math.tau*92+np.sin(v*math.tau*66)*.9)
        delta = .055*folds+.045*knit+.029*fleck
        height = (.13 if color == 'body_ribbing' else .078)*knit+.09*folds+.055*fleck
        gloss = .28 if color=='body_canvas' else .16
    elif color in ('skin_body','body_nail'):
        delta = .045*np.cos((2*u+3*v)*math.tau)+.029*fleck
        height = .036*fleck
        gloss = .38 if color=='skin_body' else .46
    elif color in ('body_hair','body_hair_light','body_beard'):
        flow = np.cos((26*u+4*v)*math.tau + .28*np.sin(v*18))
        delta = .115*flow+.073*fleck
        height = .08*flow+.035*fleck
        gloss = .35
    else:
        delta = .02*fleck
        height = .035*fleck
    return np.clip(shades[None,None,:]*(1+delta[:,:,None]),0,1),.5+height,gloss


def face_data(api, width, height, random):
    u = np.linspace(0,1,width,dtype=np.float32)[None,:]
    t = np.linspace(0,1,height,dtype=np.float32)[:,None]
    a = (u-.5)*math.tau/.992
    z = 1.455 + ((t-.028)/.946)*.353
    abs_a = abs(a)
    noise = random.uniform(-1,1,(height,width)).astype(np.float32)
    warm = np.exp(-((abs_a-.63)/.31)**2-((z-1.645)/.034)**2)
    eyes = np.exp(-((abs_a-.35)/.22)**2-((z-1.663)/.019)**2)
    ears = np.exp(-((abs_a-1.54)/.28)**2-((z-1.653)/.037)**2)
    dark = .037*eyes+.03*ears+.03*noise+.019*np.cos(24*a+80*z)
    skin = np.array(api.rgba('skin_body')[:3],dtype=np.float32)
    albedo = skin[None,None,:]*(1-dark[:,:,None])
    albedo += (.049*warm+.072*ears)[:,:,None]*np.array([1,-.32,-.48],dtype=np.float32)
    # Trimmed beard follows the jaw, with lower density at the cheek boundary.
    beard_top = 1.613+.047*smooth(.27,1.10,abs_a)-.030*smooth(1.26,1.71,abs_a)
    jaw = smooth(1.527,1.568,z)*(1-smooth(beard_top-.012,beard_top+.009,z))*(1-smooth(1.28,1.55,abs_a))
    mustache = np.exp(-((z-(1.621-.015*abs_a))/.0077)**2)*smooth(.015,.079,abs_a)*(1-smooth(.31,.52,abs_a))
    stubble = np.maximum(jaw,mustache) * np.clip(.72+.25*noise+.051*np.sin(a*151+z*592+3*np.cos(a*42+z*172)),.34,.99)
    lip = np.exp(-((z-1.607)/.0065)**2)*(1-smooth(.19,.33,abs_a))
    stubble *= 1-.76*lip
    brow_z = 1.691+.010*np.exp(-((abs_a-.40)/.28)**2)
    brow = np.exp(-((z-brow_z)/.0036)**2)*smooth(.13,.24,abs_a)*(1-smooth(.55,.76,abs_a))
    brow *= np.clip(.74+.31*noise+.18*np.cos(abs_a*230+z*132),.25,1)
    lids = np.exp(-((z-(1.663+.006*(abs_a-.37)))/.0018)**2)*smooth(.15,.21,abs_a)*(1-smooth(.45,.55,abs_a))
    hair = np.clip(stubble+.76*brow+.46*lids,0,1)
    beard_color = np.array([.186,.116,.089],dtype=np.float32)
    albedo = albedo*(1-hair[:,:,None])+beard_color[None,None,:]*hair[:,:,None]
    albedo += lip[:,:,None]*np.array([-.068,-.13,-.097],dtype=np.float32)
    heights = .5+.026*noise+.045*hair
    gloss = .41+.07*lip-.13*stubble
    return np.clip(albedo,0,1), heights, gloss


def make_detail_atlas(api, base_material):
    size = 2048
    textures = {name: np.ones((size,size,4),dtype=np.float32) for name in ('Albedo','Height','Surface')}
    random = np.random.default_rng(379)

    def assign(x, y, w, h, data, metal=0):
        color, height, gloss = data
        textures['Albedo'][y:y+h,x:x+w,:3] = color
        textures['Height'][y:y+h,x:x+w,:3] = height[:,:,None]
        textures['Surface'][y:y+h,x:x+w,:3] = metal
        textures['Surface'][y:y+h,x:x+w,3] = gloss

    for i in range(64):
        shade = api.COLORS[i] if i < len(api.COLORS) else 'ink'
        n = 128
        u = np.linspace(0,1,n,dtype=np.float32)[None,:]
        v = u.T
        color = np.array(api.rgba(shade)[:3],dtype=np.float32)
        assign(i%8*n,i//8*n,n,n,tile_data(shade,color,u,v,random),.45 if shade=='brass' else 0)
    for shade, (x,y) in FABRIC_TILES.items():
        n = 512
        u = np.linspace(0,1,n,dtype=np.float32)[None,:]
        color = np.array(api.rgba(shade)[:3],dtype=np.float32)
        assign(x*n,y*n,n,n,tile_data(shade,color,u,u.T,random))
    assign(0,1024,2048,1024,face_data(api,2048,1024,random))

    images = {}
    for suffix, pixels in textures.items():
        label = f'HideIT_Body{suffix}'
        image = bpy.data.images.new(label,width=size,height=size,alpha=suffix=='Surface')
        image.colorspace_settings.name = 'sRGB' if suffix=='Albedo' else 'Non-Color'
        image.pixels.foreach_set(pixels.ravel())
        image.filepath_raw = str(api.EXPORT / 'Textures' / f'{label}.png')
        image.file_format = 'PNG'
        image.save()
        images[suffix] = image

    material = base_material.copy()
    material.name = 'HideIT_Body'
    nodes = material.node_tree.nodes
    for node in nodes:
        if node.type=='TEX_IMAGE' and node.image:
            suffix = 'Albedo' if node.image.name.startswith('HIdeIT_Palette') else 'Surface'
            node.image = images[suffix]
            node.interpolation = 'Linear'
    height = nodes.new('ShaderNodeTexImage')
    height.image = images['Height']
    bump = nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = .32
    bump.inputs['Distance'].default_value = .003
    material.node_tree.links.new(height.outputs['Color'],bump.inputs['Height'])
    material.node_tree.links.new(bump.outputs['Normal'],nodes.get('Principled BSDF').inputs['Normal'])
    return material
