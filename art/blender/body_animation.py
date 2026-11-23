"""Editable pose clips for the Hide iT body rig."""

import math

import bpy
from mathutils import Euler, Matrix, Vector


def add_finger_bones(rig, specs):
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    for name, start, end, parent in specs:
        bone = rig.data.edit_bones.new(name)
        bone.head, bone.tail = start, end
        bone.parent = rig.data.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')


def key_pose(rig, frame, root, joints, curl):
    y, z, tilt = root
    for bone in rig.pose.bones:
        bone.rotation_mode = 'QUATERNION'
        basis = bone.bone.matrix_local.to_quaternion()
        if bone.name == 'Root':
            bone.location = basis.inverted() @ Vector((0,y,z))
            bone.keyframe_insert(data_path='location', frame=frame, group=bone.name)
            rotation = (tilt,0,0)
        elif bone.name[-1].isdigit():
            rotation = (curl*(.58 if 'Thumb' in bone.name else 1),0,0)
        else:
            rotation = joints.get(bone.name,(0,0,0))
        world = Euler(tuple(math.radians(value) for value in rotation),'XYZ').to_quaternion()
        bone.rotation_quaternion = basis.inverted() @ world @ basis
        bone.keyframe_insert(data_path='rotation_quaternion', frame=frame, group=bone.name)


def create_actions(rig):
    rest = {
        'Head':(1,0,-12), 'Chest':(2,0,-2), 'LeftUpperArm':(-6,-9,0),
        'RightUpperArm':(5,8,0), 'LeftLowerArm':(-15,0,0), 'RightLowerArm':(-21,0,5),
        'LeftUpperLeg':(-2,-4,0), 'RightUpperLeg':(-4,4,0), 'LeftLowerLeg':(4,0,0),
        'RightLowerLeg':(8,0,0), 'RightFoot':(-5,0,0), 'LeftFoot':(8,0,0)
    }
    collapsed = {**rest, 'Head':(7,-4,-17), 'LeftLowerArm':(-24,0,4), 'RightUpperLeg':(-9,9,0)}
    pull = {**rest, 'Head':(10,-3,-7), 'LeftLowerArm':(-11,0,1), 'RightLowerArm':(-10,0,5)}
    clips = {
        'Body_Collapse': [
            (1,(0,0,0),{},2),
            (8,(.015,-.095,-9),{'Head':(14,0,7),'Chest':(13,0,1),
                 'LeftUpperLeg':(-18,0,0),'RightUpperLeg':(-19,0,0),
                 'LeftLowerLeg':(31,0,0),'RightLowerLeg':(33,0,0)},6),
            (15,(-.09,-.20,-35),{'Head':(21,0,1),'Chest':(11,0,-4),
                 'LeftUpperLeg':(-32,0,0),'RightUpperLeg':(-25,0,0),
                 'LeftLowerLeg':(45,0,0),'RightLowerLeg':(44,0,0),
                 'LeftUpperArm':(-14,-12,1),'RightUpperArm':(-17,9,-2),
                 'LeftLowerArm':(-20,0,1),'RightLowerArm':(-27,0,1)},8),
            (25,(-.59,.254,-83),collapsed,9), (30,(-.89,.168,-90),collapsed,6),
            (37,(-.90,.174,-89),{**rest,'Head':(3,-2,-14)},7),
            (52,(-.90,.164,-90),rest,5),
        ],
        'Body_Setting_Down': [
            (1,(-.90,.28,-81),pull,6), (9,(-.90,.219,-86),pull,7),
            (19,(-.90,.164,-90),collapsed,8), (26,(-.90,.17,-89),rest,7),
            (43,(-.90,.164,-90),rest,5),
        ],
        'Body_Head_Drag_Loop': [
            (1,(-.90,.191,-87),pull,7),
            (9,(-.90,.191,-86),{**pull,'Head':(12,-3,-5),'RightLowerArm':(-13,0,5)},6),
            (17,(-.90,.195,-85),{**pull,'Head':(11,3,-8),'LeftUpperLeg':(-4,-5,0),'LeftLowerArm':(-14,0,5)},8),
            (25,(-.90,.191,-88),{**pull,'Head':(8,0,-9),'LeftUpperLeg':(-1,-3,0)},7),
            (33,(-.90,.191,-87),pull,7),
        ],
    }
    animation = rig.animation_data_create()
    for name, frames in clips.items():
        action = bpy.data.actions.new(name)
        animation.action = action
        for frame, root, joints, curl in frames:
            key_pose(rig, frame, root, joints, curl)
        action.use_fake_user = True
    animation.action = None
    for bone in rig.pose.bones:
        bone.matrix_basis = Matrix.Identity(4)
    return clips
