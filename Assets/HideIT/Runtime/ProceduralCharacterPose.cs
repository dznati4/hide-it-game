using System.Collections.Generic;
using UnityEngine;

namespace HideIT
{
    [DefaultExecutionOrder(100)]
    public sealed class ProceduralCharacterPose : MonoBehaviour
    {
        sealed class Bone
        {
            public Transform Joint;
            public Quaternion Rest;
            public Quaternion RootToLocal;
        }

        sealed class Limb
        {
            public Transform Root, Mid, Tip;
            public Vector3 RestPosition;
            public Quaternion RestRotation;
            public float UpperLength, LowerLength;
            public Vector3 PalmOffset;
            public Quaternion HandFrame;
        }

        readonly Dictionary<string, Bone> bones = new Dictionary<string, Bone>();
        readonly Limb[] arms = new Limb[2];
        readonly Limb[] legs = new Limb[2];
        readonly float[] armTuck = new float[2];
        Transform hips;
        Vector3 restHips, velocity, smoothedVelocity, gripPoint, gripAxis, gripLean;
        float cycle, idleTime, crouchWeight, gripWeight, liftWeight, gripStoop;
        bool crouched, holding, raised, headGrip;
        const int GroundLayer = 1 << 0;

        internal float GripWeight => gripWeight;
        internal float PalmError { get; private set; }

        public void Initialize()
        {
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (bones.ContainsKey(child.name)) continue;
                bones.Add(child.name, new Bone {
                    Joint = child,
                    Rest = child.localRotation,
                    RootToLocal = Quaternion.Inverse(Quaternion.Inverse(transform.rotation) * child.rotation)
                });
            }
            hips = bones["Hips"].Joint;
            restHips = hips.localPosition;
            for (int i = 0; i < 2; i++)
            {
                string side = i == 0 ? "Left" : "Right";
                float sign = i == 0 ? -1f : 1f;
                var arm = arms[i] = ReadLimb(side, "UpperArm", "LowerArm", "Hand");
                legs[i] = ReadLimb(side, "UpperLeg", "LowerLeg", "Foot");
                var shoulderLine = transform.InverseTransformDirection(arm.Mid.position - arm.Root.position);
                armTuck[i] = Mathf.Clamp(Mathf.Atan2(Mathf.Abs(shoulderLine.x), -shoulderLine.y) * Mathf.Rad2Deg - 8f, 0, 31);
                var fingers = (arm.Tip.position - arm.Mid.position).normalized;
                var palmNormal = Vector3.ProjectOnPlane(-sign * transform.right, fingers).normalized;
                arm.PalmOffset = Quaternion.Inverse(arm.Tip.rotation) * (fingers * .055f);
                arm.HandFrame = Quaternion.Inverse(Quaternion.LookRotation(fingers, palmNormal)) * arm.Tip.rotation;
            }
        }

        Limb ReadLimb(string side, string upper, string lower, string tip)
        {
            var limb = new Limb { Root = bones[side + upper].Joint, Mid = bones[side + lower].Joint, Tip = bones[side + tip].Joint };
            limb.UpperLength = Vector3.Distance(limb.Root.position, limb.Mid.position);
            limb.LowerLength = Vector3.Distance(limb.Mid.position, limb.Tip.position);
            limb.RestPosition = transform.InverseTransformPoint(limb.Tip.position);
            limb.RestRotation = Quaternion.Inverse(transform.rotation) * limb.Tip.rotation;
            return limb;
        }

        public void SetMotion(Vector3 planarVelocity, bool crouch, bool carry, bool lift)
        {
            velocity = planarVelocity;
            crouched = crouch;
            holding = carry;
            raised = lift;
        }

        internal void SetGrip(Vector3 point, Vector3 axis, bool head)
        {
            gripPoint = point;
            gripAxis = axis;
            headGrip = head;
        }

        internal void ResetMotion()
        {
            SetMotion(Vector3.zero, false, false, false);
            smoothedVelocity = Vector3.zero;
            gripLean = Vector3.zero;
            cycle = idleTime = crouchWeight = gripWeight = liftWeight = gripStoop = PalmError = 0;
        }

        void Turn(string name, Vector3 rootAxis, float angle)
        {
            var bone = bones[name];
            bone.Joint.localRotation *= Quaternion.AngleAxis(angle, bone.RootToLocal * rootAxis);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (hips == null || dt <= 0) return;
            smoothedVelocity = Vector3.Lerp(smoothedVelocity, velocity, 1f - Mathf.Exp(-14f * dt));
            crouchWeight = Mathf.MoveTowards(crouchWeight, crouched ? 1f : 0f, 6f * dt);
            gripWeight = Mathf.MoveTowards(gripWeight, holding ? 1f : 0f, (holding ? 7f : 6f) * dt);
            liftWeight = Mathf.MoveTowards(liftWeight, raised ? 1f : 0f, 5f * dt);
            idleTime = Mathf.Repeat(idleTime + dt, 60);
            float speed = smoothedVelocity.magnitude;
            float gait = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.12f, .9f, speed));
            float run = Mathf.InverseLerp(3.6f, CarryLabPlayer.RunSpeed, speed);
            float cadence = Mathf.Lerp(1.12f, 2.7f, Mathf.InverseLerp(.8f, CarryLabPlayer.RunSpeed, speed));
            float stanceTime = Mathf.Lerp(.63f, .45f, run);
            float travel = Mathf.Min(.94f, speed * stanceTime / cadence);
            cycle = Mathf.Repeat(cycle + cadence * gait * dt, 1);
            float phase = cycle * 2f * Mathf.PI;
            float breath = Mathf.Sin(idleTime * 2.3f) * (1f - gait);
            float carryingLow = gripWeight * (1f - liftWeight);

            foreach (var bone in bones.Values) bone.Joint.localRotation = bone.Rest;
            float lowGrip = transform.position.y + .69f - gripPoint.y;
            float desiredStoop = gripWeight * Mathf.Clamp(lowGrip * .88f, 0, .26f);
            gripStoop = Mathf.Lerp(gripStoop,desiredStoop,1f-Mathf.Exp(-18f*dt));
            float kneeClearance = .10f * gait + Mathf.Lerp(.31f, .19f, liftWeight) * gripWeight + gripStoop;
            float pelvisDrop = Mathf.Max(kneeClearance, .37f * crouchWeight);
            float bob = Mathf.Cos(phase * 2f) * gait * Mathf.Lerp(.014f, .036f, run);
            hips.localPosition = restHips;
            hips.position += transform.up * (-pelvisDrop + bob + .006f * breath)
                + transform.right * (Mathf.Sin(phase) * .024f * gait);

            Turn("Hips", Vector3.up, Mathf.Sin(phase) * gait * 5f);
            Turn("Hips", Vector3.forward, Mathf.Cos(phase) * gait * 2.7f);
            Turn("Spine", Vector3.right, 8f * crouchWeight + 12f * carryingLow + 3f * run + breath * 1.4f);
            Turn("Chest", Vector3.right, 10f * crouchWeight + 17f * carryingLow + 5f * run + 9f * gripWeight * liftWeight);
            Turn("Chest", Vector3.up, -Mathf.Sin(phase) * gait * 10f * (1f - gripWeight * .75f));
            Turn("Head", Vector3.right, -7f * crouchWeight + 4f * carryingLow - 3f * run);

            var shoulders = (arms[0].Root.position + arms[1].Root.position) * .5f;
            var reach = gripPoint - shoulders;
            float extraReach = Mathf.Clamp(reach.magnitude - .41f, 0f, .21f);
            gripLean = Vector3.Lerp(gripLean, reach.normalized * extraReach * gripWeight, 1f - Mathf.Exp(-22f * dt));
            hips.position += gripLean;

            Vector3 travelDirection = speed > .02f ? smoothedVelocity / speed : transform.forward;
            for (int i = 0; i < 2; i++)
            {
                float sign = i == 0 ? -1f : 1f;
                string side = i == 0 ? "Left" : "Right";
                float footPhase = Mathf.Repeat(cycle + i * .5f, 1);
                PoseLeg(legs[i],travelDirection,footPhase,stanceTime,travel,gait,run);
                float swing = Mathf.Cos(phase + i * Mathf.PI) * gait * (1f - gripWeight);
                Turn(side + "UpperArm", Vector3.forward, -sign * armTuck[i]);
                Turn(side + "UpperArm", Vector3.right, swing * Mathf.Lerp(24f, 40f, run) - 9f * gripWeight);
                Turn(side + "LowerArm", Vector3.right, -(9f + 35f * run + 9f * crouchWeight) - 8f * Mathf.Max(0,swing));
            }
            PalmError = 0;
            if (gripWeight > 0) PoseGrip();
        }

        void PoseLeg(Limb leg, Vector3 direction, float phase, float stanceTime, float travel, float gait, float run)
        {
            float offset, lift = 0, angle;
            if (phase < stanceTime)
            {
                float step = phase / stanceTime;
                offset = Mathf.Lerp(.5f, -.5f, step);
                angle = -14f * Mathf.Pow(1f - step, 5f) + 16f * Mathf.Pow(step, 4f);
            }
            else
            {
                float step = (phase - stanceTime) / (1f - stanceTime);
                offset = Mathf.SmoothStep(-.5f, .5f, step);
                lift = Mathf.Sin(Mathf.PI * step) * Mathf.Lerp(.14f, .29f, run) * (1f - .45f * crouchWeight);
                angle = Mathf.Lerp(16f, -14f, step);
            }
            var foot = transform.TransformPoint(leg.RestPosition) + direction * (offset * travel * gait);
            var sole = foot - Vector3.up * leg.RestPosition.y;
            var up = Vector3.up;
            if (Physics.Raycast(sole + Vector3.up * .45f, Vector3.down, out var ground, .86f, GroundLayer, QueryTriggerInteraction.Ignore))
            {
                foot.y = ground.point.y + leg.RestPosition.y;
                up = ground.normal;
            }
            foot.y += lift * gait;
            SolveLimb(leg,foot,transform.forward,1f);
            leg.Tip.rotation = Quaternion.FromToRotation(Vector3.up,up) * transform.rotation *
                Quaternion.AngleAxis(angle * gait,Vector3.right) * leg.RestRotation;
        }

        void PoseGrip()
        {
            var across = Vector3.ProjectOnPlane(transform.right, gripAxis);
            across.y = Mathf.Clamp(across.y, -.42f, .42f);
            across = across.sqrMagnitude > .08f ? across.normalized : transform.right;
            float spacing = headGrip ? .077f : .043f;
            for (int i = 0; i < 2; i++)
            {
                float sign = i == 0 ? -1f : 1f;
                var arm = arms[i];
                var palm = gripPoint + across * (spacing * sign) + Vector3.up * .018f;
                var towardGrip = (palm - arm.Root.position).normalized;
                var fingers = (towardGrip - across * (.29f * sign) + transform.forward * .22f).normalized;
                var palmNormal = Vector3.ProjectOnPlane(-across * sign - Vector3.up * .34f,fingers);
                if (palmNormal.sqrMagnitude < .0001f) palmNormal = Vector3.ProjectOnPlane(transform.up,fingers);
                var handRotation = Quaternion.LookRotation(fingers,palmNormal) * arm.HandFrame;
                var wrist = palm - handRotation * arm.PalmOffset;
                var pole = transform.right * sign - transform.forward * .52f - transform.up * .26f;
                var baseRotation = arm.Tip.rotation;
                SolveLimb(arm,wrist,pole,gripWeight);
                arm.Tip.rotation = Quaternion.Slerp(baseRotation,handRotation,gripWeight);
                PalmError = Mathf.Max(PalmError,Vector3.Distance(palm,arm.Tip.position + arm.Tip.rotation * arm.PalmOffset));
            }
        }

        // The target and pole define the plane. The cosine rule finds the elbow/knee in that plane.
        static void SolveLimb(Limb limb, Vector3 target, Vector3 pole, float weight)
        {
            var root = limb.Root.position;
            var reach = target - root;
            if (reach.sqrMagnitude < .00001f) return;
            var direction = reach.normalized;
            var bend = Vector3.ProjectOnPlane(pole,direction);
            if (bend.sqrMagnitude < .00001f) bend = Vector3.ProjectOnPlane(limb.Mid.position - root,direction);
            if (bend.sqrMagnitude < .00001f) return;
            float a = limb.UpperLength;
            float b = limb.LowerLength;
            float length = Mathf.Clamp(reach.magnitude,Mathf.Abs(a - b) + .001f,a + b - .004f);
            float along = (a*a + length*length - b*b) / (2f * length);
            float outward = Mathf.Sqrt(Mathf.Max(0,a*a - along*along));
            var mid = root + direction * along + bend.normalized * outward;
            var tip = root + direction * length;
            var upper = Quaternion.FromToRotation(limb.Mid.position - root,mid - root);
            limb.Root.rotation = Quaternion.Slerp(Quaternion.identity,upper,weight) * limb.Root.rotation;
            var lower = Quaternion.FromToRotation(limb.Tip.position - limb.Mid.position,tip - limb.Mid.position);
            limb.Mid.rotation = Quaternion.Slerp(Quaternion.identity,lower,weight) * limb.Mid.rotation;
        }
    }
}
