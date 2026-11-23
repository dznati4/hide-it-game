"""Original grounded stylized adult character authoring; no external meshes."""
import bpy
import math
from mathutils import Vector

OUTFITS = [
    ('Player_Alex_Orange','Alex','jacket','orange','navy','skin_mid','hair_brown'),
    ('Player_Noam_Olive','Noam','hoodie','olive','ink','skin_dark','hair'),
    ('Player_Maya_Denim','Maya','denim','denim','ink','skin_light','hair_brown'),
    ('Player_Eli_Cream','Eli','shirt','cream','navy','skin_mid','hair'),
    ('Player_Roni_Plum','Roni','track','plum','slate','skin_light','hair'),
]
CITIZENS = [
    ('Civilian_Amir_RustJacket','Amir','jacket','rust','ink','skin_mid','hair_brown','beard'),
    ('Civilian_Ben_CreamShirt','Ben','shirt','cream','denim','skin_light','hair_brown','sweep'),
    ('Civilian_Lior_OliveHoodie','Lior','hoodie','olive','slate','skin_dark','hair','buzz'),
    ('Civilian_Tamar_RoseCardigan','Tamar','cardigan','rose','navy','skin_mid','hair_brown','pony'),
    ('Civilian_Nora_NavyCoat','Nora','coat','navy','cream','skin_light','hair','bob'),
    ('Civilian_Ruth_TealCoat','Ruth','coat','teal','ink','skin_mid','grey_hair','senior'),
]

def build(api,name,role,variant=0):
    start=set(bpy.data.objects)
    if role=='player':
        _,display,cut,cloth,pants,skin,hair=OUTFITS[variant]
        style=['sweep','buzz','pony','sweep','short'][variant]
    elif role=='civilian' and variant>0:
        _,display,cut,cloth,pants,skin,hair,style=CITIZENS[variant-1]
    else:
        display=role.title()
        cut={'guard':'uniform','civilian':'cardigan','body':'overshirt'}[role]
        cloth={'guard':'navy','civilian':'plum','body':'blue'}[role]
        pants={'guard':'ink','civilian':'cream','body':'navy'}[role]
        skin={'guard':'skin_dark','civilian':'skin_light','body':'skin_mid'}[role]
        hair='hair_brown' if role=='civilian' else 'hair'
        style='pony' if role=='civilian' else 'short'
    box,sphere,cylinder,loft=api.box,api.sphere,api.cylinder,api.loft
    torso=[
        ((0,0,.96),.192,.125,{'Hips':.65,'Spine':.35}),
        ((0,0,1.06),.188,.128,{'Spine':1}),
        ((0,0,1.18),.216,.142,{'Spine':.5,'Chest':.5}),
        ((0,0,1.31),.243,.15,{'Chest':1}),
        ((0,0,1.41),.244,.135,{'Chest':1}),
        ((0,0,1.47),.17,.11,{'Chest':.8,'Neck':.2}),
        ((0,0,1.50),.083,.073,{'Neck':.7,'Chest':.3})]
    if cut=='hoodie':
        torso=[(p,w*1.08,d*1.08,weights) for p,w,d,weights in torso]
    elif cut=='shirt':
        torso=[(p,w*.98,d*.98,weights) for p,w,d,weights in torso]
    elif cut=='coat':
        torso.insert(0,((0,0,.76),.22,.14,{'Hips':1}))
    loft('Tailored torso',torso,cloth)
    box('Trouser waist',(0,0,.93),(.36,.23,.17),pants,.035,'Hips')
    cylinder('Neck',(0,0,1.555),.064,.15,skin,'Neck')
    # The face is part of a shaped cranium: narrow chin, jaw, cheek and brow planes.
    loft('Adult head',[
        ((0,-.015,1.574),.047,.055,{'Head':1}),
        ((0,-.005,1.593),.071,.073,{'Head':1}),
        ((0,.005,1.627),.096,.089,{'Head':1}),
        ((0,.010,1.672),.108,.102,{'Head':1}),
        ((0,.013,1.718),.104,.10,{'Head':1}),
        ((0,.016,1.76),.089,.085,{'Head':1}),
        ((0,.016,1.79),.050,.055,{'Head':1}),
        ((0,.016,1.804),.008,.012,{'Head':1})],skin)
    sphere('Integrated nose bridge',(0,-.096,1.672),(.014,.016,.033),skin,'Head')
    sphere('Nose tip',(0,-.115,1.647),(.021,.023,.014),skin,'Head')
    for sign in (-1,1):
        sphere('Ear',(sign*.108,.013,1.675),(.016,.022,.033),skin,'Head')
        if role=='body':
            box('Closed eyelid',(sign*.045,-.087,1.685),(.030,.007,.0035),'hair',.001,'Head')
        else:
            sphere('Inset eye',(sign*.045,-.088,1.685),(.018,.007,.009),'eye_white','Head')
            sphere('Iris',(sign*.045,-.0945,1.685),(.0065,.0025,.007),'eye_dark','Head')
            lid=box('Upper eyelid',(sign*.045,-.091,1.693),(.034,.006,.004),skin,.001,'Head')
            brow=box('Brow',(sign*.047,-.084,1.709),(.036,.008,.006),hair,.002,'Head')
            brow.rotation_euler[1]=sign*.09
    box('Upper lip',(0,-.089,1.615),(.037,.004,.0035),'lip',.001,'Head')
    sphere('Lower lip',(0,-.088,1.610),(.018,.003,.003),skin,'Head')
    # Layered irregular hair masses replace the previous rectangular cap.
    sphere('Crown hair',(0,.021,1.777),(.097,.085,.014 if style=='buzz' else .034),hair,'Head')
    if style!='buzz':
        for i in range(5):
            x=(i-2)*.032
            sphere('Hair sweep',(x,-.050+abs(x)*.12,1.765+(i%2)*.006),(.030,.037,.025),hair,'Head')
    if style=='bob': sphere('Bob hair',(0,.065,1.695),(.120,.085,.115),hair,'Head')
    if style=='senior': sphere('Hair bun',(0,.115,1.753),(.055,.05,.05),hair,'Head')
    if style=='beard':
        for x in (-.045,0,.045): sphere('Short beard',(x,-.060,1.599),(.035,.034,.016),hair,'Head')
    if style=='senior':
        for sign in (-1,1):
            bpy.ops.mesh.primitive_torus_add(major_radius=.023,minor_radius=.0022,major_segments=16,
                minor_segments=6,location=(sign*.045,-.103,1.685),rotation=(math.pi/2,0,0))
            api.finish(bpy.context.object,'Glasses frame','brass','Head')
        box('Glasses bridge',(0,-.104,1.686),(.045,.004,.004),'brass',.001,'Head')
    for side,sign in (('Left',1),('Right',-1)):
        upper=side+'UpperArm'
        lower=side+'LowerArm'
        sleeve=api.jointed_limb(side+' continuous sleeve',(sign*.24,0,1.43),(sign*.40,0,1.13),
            (sign*.53,0,.88),.17 if cut!='hoodie' else .19,.205,cloth,upper,lower)
        # Blend garment roots towards the clavicle; keep the torso a separate garment island.
        group=sleeve.vertex_groups.new(name=side+'Shoulder')
        group.add(list(range(12)),.35,'REPLACE')
        sleeve.vertex_groups[upper].add(list(range(12)),.65,'REPLACE')
        sphere('Palm',(sign*.55,-.007,.83),(.042,.027,.062),skin,side+'Hand')
        for finger in range(4):
            sphere('Finger',(sign*(.53+finger*.014),-.014,.772+abs(finger-1.5)*.003),
                (.008,.012,.034),skin,side+'Hand')
        sphere('Thumb',(sign*.514,-.033,.825),(.017,.020,.034),skin,side+'Hand')
        api.jointed_limb(side+' continuous trouser',(sign*.14,0,.94),(sign*.14,0,.53),
            (sign*.14,0,.15),.19,.235,pants,side+'UpperLeg',side+'LowerLeg')
        shoe=box('Shaped shoe',(sign*.14,-.055,.088),(.145,.29,.145),'rubber',.036,side+'Foot')
        toe=shoe.vertex_groups.new(name=side+'Toes')
        foot=shoe.vertex_groups[side+'Foot']
        for vertex in shoe.data.vertices:
            # Mesh coordinates are local to the shoe origin until joined.
            if vertex.co.y<-.075:
                toe.add([vertex.index],.7,'REPLACE')
                foot.add([vertex.index],.3,'REPLACE')
        box('Sole',(sign*.14,-.055,.027),(.15,.30,.035),'cream' if role!='guard' else 'slate',.008,side+'Foot')
        box('Cuff',(sign*.526,0,.897),(.138,.15,.035),cloth,.01,lower)
    # Thin seams/pockets sit on the garment surface, rather than raised armour panels.
    if cut not in ('hoodie','shirt'):
        box('Shirt opening',(0,-.150,1.30),(.075,.008,.29),'cream',.002,'Chest')
        for sign in (-1,1):
            lapel=box('Collar',(sign*.053,-.139,1.448),(.062,.014,.092),cloth,.005,'Chest')
            lapel.rotation_euler[1]=sign*.28
            box('Flush pocket',(sign*.145,-.146,1.335),(.080,.009,.067),cloth,.003,'Chest')
            box('Pocket seam',(sign*.145,-.153,1.366),(.076,.004,.004),'dark_wood',.001,'Chest')
        box('Jacket seam',(0,-.133,1.09),(.008,.006,.15),'dark_wood',.002,'Spine')
    if cut=='hoodie':
        # A broad hood outline differentiates this player even in grayscale.
        sphere('Hood back',(0,.098,1.47),(.139,.098,.112),cloth,'Chest')
        box('Kangaroo pocket',(0,-.149,1.10),(.25,.012,.14),cloth,.025,'Spine')
        for sign in (-1,1): cylinder('Drawstring',(sign*.045,-.15,1.40),.005,.13,'cream','Chest',8)
    if style=='pony':
        sphere('Ponytail',(0,.135,1.705),(.047,.080,.093),hair,'Head')
    if cut=='shirt':
        for sign in (-1,1): box('Rolled cuff',(sign*.50,0,.97),(.16,.18,.075),cloth,.012,sign>0 and 'LeftLowerArm' or 'RightLowerArm')
        for i in range(3): box('Shirt fold',(0,-.141,1.08+i*.025),(.19,.006,.005),'white',.001,'Spine')
    if cut=='track':
        box('Zip',(0,-.153,1.29),(.008,.006,.34),'cream',.001,'Chest')
        for sign in (-1,1): box('Shoulder stripe',(sign*.195,-.091,1.425),(.044,.10,.025),'cream',.004,'Chest')
    if role=='guard':
        cylinder('Peaked cap',(0,.012,1.827),.128,.065,'navy','Head')
        box('Peaked brim',(0,-.105,1.806),(.22,.15,.019),'ink',.013,'Head')
        box('Fictional hotel badge',(-.13,-.160,1.355),(.034,.009,.049),'brass',.006,'Chest')
        box('Duty belt',(0,0,.965),(.39,.258,.050),'rubber',.008,'Hips')
        box('Radio',(.22,-.01,1.07),(.055,.07,.14),'ink',.01,'Hips')
        cylinder('Antenna',(.22,-.01,1.16),.006,.09,'ink','Hips',8)
        for sign in (-1,1): box('Shoulder yoke',(sign*.222,0,1.465),(.105,.19,.025),'slate',.006,'Chest')
    if role=='civilian' and style in ('pony','bob','senior'):
        box('Shoulder bag',(.27,.025,1.06),(.17,.17,.22),'wood',.025,'Hips')
        strap=box('Bag strap',(.16,-.151,1.31),(.025,.012,.39),'wood',.003,'Chest')
        strap.rotation_euler[1]=-.3
    meshes=[o for o in set(bpy.data.objects)-start if o.type=='MESH']
    mesh=api.join(meshes,name+'_Mesh')
    rig=api.make_rig(name)
    modifier=mesh.modifiers.new('Hide iT adult skin','ARMATURE')
    modifier.object=rig
    mesh.parent=rig
    for marker_name,location in [('FaceForward',(0,-.4,1.7)),('GripHead',(0,0,1.79)),
        ('GripLeftHand',(.55,0,.79)),('GripRightHand',(-.55,0,.79)),
        ('GripLeftAnkle',(.14,0,.15)),('GripRightAnkle',(-.14,0,.15))]:
        marker=bpy.data.objects.new(name+'__'+marker_name,None)
        bpy.context.collection.objects.link(marker)
        marker.parent=rig
        marker.location=location
    rig['role']=role
    rig['display_name']=display
    rig['prototype_note']='Original adult style study; connected limbs, separate garment islands at shoulders/hips.'
    return rig,[rig,mesh]+[o for o in set(bpy.data.objects)-start if o.type=='EMPTY']
