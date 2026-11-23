"""A small original hotel/parking kit and cutaway diorama, all measured in meters."""
import bpy
import math
from mathutils import Vector

def build(a):
    scene=bpy.data.scenes.new('Hide iT World Study')
    bpy.context.window.scene=scene
    scene.unit_settings.system='METRIC'
    scene.unit_settings.scale_length=1
    box,cylinder,sphere=a.box,a.cylinder,a.sphere
    assets={}
    def asset(name,fn):
        obj=a.prop(name,fn)
        obj.hide_render=True
        assets[name]=obj
        return obj
    def wall():
        box('Plaster',(1,.09,1.4),(2,.18,2.8),'cream',.012)
        box('Skirting',(1,-.018,.07),(2,.045,.14),'dark_wood',.008)
        box('Dado',(1,-.025,1.05),(2,.035,.055),'wood',.006)
    asset('Hotel_Wall_2m',wall)
    asset('Hotel_Floor_2m',lambda:box('Tile',(1,1,-.06),(2,2,.12),'wood',.008))
    asset('Parking_Floor_2m',lambda:box('Concrete',(1,1,-.06),(2,2,.12),'floor',.008))
    def doorway():
        for sign in (-1,1):
            box('Doorpost',(sign*.875,0,1.25),(.25,.20,2.5),'teal',.018)
        box('Lintel',(0,0,2.65),(2,.20,.30),'teal',.018)
        box('Sign',(0,-.13,2.68),(.63,.05,.15),'cream',.01)
        for x in (-.15,0,.15): box('Sign glyph',(x,-.16,2.68),(.045,.012,.065),'ink',.004)
    asset('Doorframe_Clearance_1p5m',doorway)
    def service_door():
        box('Door leaf',(.74,0,1.24),(1.48,.085,2.48),'teal',.02)
        box('Push plate',(1.13,-.052,1.1),(.12,.025,.3),'slate',.008)
        cylinder('Handle',(1.15,-.09,1.1),.025,.16,'brass',rotation=(math.pi/2,0,0))
    asset('Service_Door_HingeOrigin',service_door)
    def locker():
        box('Locker shell',(0,0,.95),(.75,.60,1.9),'teal',.035)
        box('Door',(0,-.312,.96),(.69,.025,1.78),'mint',.012)
        for z in (1.55,1.62,1.69): box('Vent',(0,-.331,z),(.43,.012,.018),'ink',.002)
        box('Handle',(.23,-.35,.98),(.025,.04,.17),'slate',.007)
    asset('Service_Locker',locker)
    def trolley():
        box('Trolley deck',(0,0,.28),(.76,1.15,.10),'slate',.035)
        box('Rubber mat',(0,0,.34),(.68,1.08,.025),'rubber',.02)
        for x in (-.31,.31):
            for y in (-.45,.45):
                cylinder('Tyre',(x,y,.12),.11,.065,'rubber',rotation=(0,math.pi/2,0))
                cylinder('Axle',(x,y,.12),.039,.075,'slate',rotation=(0,math.pi/2,0))
            cylinder('Handle post',(x,.5,.67),.025,.68,'teal')
        box('Handle',(0,.5,1.0),(.67,.055,.055),'teal',.022)
    asset('FourWheel_CarryTrolley',trolley)
    def camera_prop():
        box('Mount',(0,.17,.2),(.14,.075,.20),'slate',.012)
        box('Arm',(0,.0,.20),(.06,.34,.055),'slate',.015)
        box('Camera housing',(0,-.18,.18),(.18,.28,.15),'cream',.035)
        cylinder('Lens',(0,-.331,.18),.043,.025,'eye_dark',rotation=(math.pi/2,0,0))
    asset('Security_Camera',camera_prop)
    def pillar():
        box('Column',(0,0,1.4),(.65,.65,2.8),'concrete',.025)
        box('Colour band',(0,-.329,.7),(.64,.018,.36),'teal',.004)
        for x in (-.19,0,.19):
            stripe=box('Warning stripe',(x,-.343,.70),(.09,.012,.35),'mustard',.002)
            stripe.rotation_euler[1]=-.25
    asset('Parking_Pillar',pillar)
    def barrier():
        box('Barrier base',(0,0,.55),(.32,.38,1.1),'teal',.045)
        box('Arm',(1.28,0,1.05),(2.6,.085,.12),'cream',.018)
        for x in (.4,.9,1.4,1.9,2.4): box('Arm stripe',(x,-.047,1.05),(.15,.014,.115),'orange',.002)
        box('Reader',(0,-.21,.88),(.17,.035,.20),'ink',.012)
    asset('Parking_Barrier_HingeOrigin',barrier)
    def car():
        box('Unbranded body',(0,0,.58),(1.62,3.70,.49),'blue',.16)
        box('Cabin',(0,.22,1.09),(1.37,1.91,.68),'blue',.18)
        glass=box('Windshield',(0,-.765,1.12),(1.20,.045,.43),'glass',.04)
        glass.rotation_euler[0]=-.28
        for sign in (-1,1):
            box('Side glass',(sign*.691,.22,1.16),(.025,1.43,.35),'glass',.035)
            box('Door handle',(sign*.814,.1,.76),(.025,.18,.027),'slate',.008)
            for y in (-1.15,1.15):
                cylinder('Wheel',(sign*.76,y,.31),.29,.18,'rubber',rotation=(0,math.pi/2,0),vertices=20)
                cylinder('Hub',(sign*.86,y,.31),.15,.025,'slate',rotation=(0,math.pi/2,0))
        for sign in (-1,1):
            box('Headlight',(sign*.53,-1.858,.66),(.34,.026,.12),'cream',.028)
            box('Rear reflector',(sign*.54,1.86,.66),(.27,.02,.11),'rose',.025)
    asset('Parked_Hatchback_Blocker',car)
    def desk():
        box('Reception',(0,0,.54),(1.8,.65,1.08),'wood',.045)
        box('Teal inset',(0,-.334,.58),(1.6,.025,.7),'teal',.025)
        box('Worktop',(0,0,1.12),(1.93,.76,.10),'cream',.04)
        box('Monitor',(0,.05,1.40),(.43,.07,.28),'ink',.022)
        box('Monitor stand',(0,.05,1.25),(.07,.07,.20),'slate',.009)
    asset('Reception_Desk',desk)

    def place(name,pos,yaw=0):
        obj=assets[name].copy()
        obj.data=assets[name].data
        bpy.context.collection.objects.link(obj)
        obj.hide_render=False
        obj.location=pos
        obj.rotation_euler[2]=math.radians(yaw)
        return obj
    for x in (-6,-4):
        for y in (-1,1): place('Hotel_Floor_2m',(x,y,0))
    place('Hotel_Wall_2m',(-6,3,0))
    place('Doorframe_Clearance_1p5m',(-3,3,0))
    for y in (-1,1): place('Hotel_Wall_2m',(-6,y,0),90)
    place('Service_Locker',(-5.5,2.4,0))
    place('Reception_Desk',(-3.9,.35,0))
    place('FourWheel_CarryTrolley',(-5.0,.3,0),15)
    place('Security_Camera',(-5.4,2.8,2.2))
    for x in (2,4):
        for y in (-1,1): place('Parking_Floor_2m',(x,y,0))
    place('Parking_Pillar',(2.45,2.5,0))
    place('Parked_Hatchback_Blocker',(4.55,1,0))
    place('Parking_Barrier_HingeOrigin',(2.1,-.7,0))
    for x in (2.3,5.65): box('Bay marking',(x,.85,.006),(.06,3.4,.012),'cream',0)
    a.label('HOTEL / PUBLIC SPACE',(-4,-1.4,-.10),.22,'cream')
    a.label('PARKING / SERVICE SPACE',(4,-1.4,-.10),.22,'mint')
    box('Backdrop',(0,0,-.31),(200,200,.35),'ink',0)
    camera=a.setup_render()
    camera.location=(11,-15,11)
    camera.rotation_euler=(Vector((0,.6,.7))-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.ortho_scale=15
    a.label('Hide iT / MODULAR WORLD STUDY',(0,4.7,3.25),.35,'mint')
    bpy.ops.wm.save_as_mainfile(filepath=str(a.SOURCE/'HIdeIT_WorldStudy.blend'))
    bpy.ops.file.make_paths_relative()
    bpy.ops.wm.save_as_mainfile(filepath=str(a.SOURCE/'HIdeIT_WorldStudy.blend'))
    scene.render.filepath=str(a.PREVIEW/'06-environment-study.png')
    bpy.ops.render.render(write_still=True)
