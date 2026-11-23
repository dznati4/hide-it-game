using System.Collections.Generic;
using UnityEngine;

namespace HideIT
{
    public sealed class RagdollBody : MonoBehaviour
    {
        public struct Grip
        {
            public Rigidbody Body;
            public Vector3 LocalPoint;
            public string Label;
            public Vector3 WorldPoint => Body.transform.TransformPoint(LocalPoint);
        }

        readonly List<Rigidbody> parts = new List<Rigidbody>();
        readonly List<Collider> colliders = new List<Collider>();
        readonly List<Vector3> restPositions = new List<Vector3>();
        readonly List<Quaternion> restRotations = new List<Quaternion>();
        readonly List<Grip> grips = new List<Grip>();
        public IReadOnlyList<Rigidbody> Parts => parts;
        public IReadOnlyList<Collider> Colliders => colliders;
        public IReadOnlyList<Grip> Grips => grips;
        public Vector3 Position => parts.Count == 0 ? transform.position : parts[0].position;

        public void Build(Vector3 spawn)
        {
            var suit = new Color(.32f, .40f, .65f);
            var skin = new Color(.85f, .65f, .44f);
            var shoes = new Color(.14f, .16f, .22f);
            var hips = Part("Hips", new Vector3(0, 1, 0), new Vector3(.43f, .27f, .27f), 12, suit);
            var chest = Part("Chest", new Vector3(0, 1.36f, 0), new Vector3(.52f, .48f, .28f), 18, suit);
            // Match the carried model's skull instead of using the old oversized placeholder.
            var head = Part("Head", new Vector3(0, 1.765f, -.02f), Vector3.one * .26f, 5, skin, true);
            Joint(chest, hips, new Vector3(0, 1.12f, 0), 25, 25);
            Joint(head, chest, new Vector3(0, 1.66f, 0), 35, 30);

            for (int side = -1; side <= 1; side += 2)
            {
                string prefix = side < 0 ? "Left " : "Right ";
                var upperArm = Part(prefix + "upper arm", new Vector3(side * .4f, 1.36f, 0), new Vector3(.18f, .38f, .18f), 2.5f, suit);
                var lowerArm = Part(prefix + "forearm", new Vector3(side * .43f, .98f, 0), new Vector3(.16f, .34f, .16f), 1.5f, skin);
                Joint(upperArm, chest, new Vector3(side * .29f, 1.53f, 0), 70, 55);
                Joint(lowerArm, upperArm, new Vector3(side * .42f, 1.17f, 0), 75, 15);
                var thigh = Part(prefix + "thigh", new Vector3(side * .14f, .69f, 0), new Vector3(.21f, .41f, .23f), 6, suit);
                var shin = Part(prefix + "shin", new Vector3(side * .14f, .27f, 0), new Vector3(.19f, .39f, .20f), 3, shoes);
                Joint(thigh, hips, new Vector3(side * .14f, .88f, 0), 60, 35);
                Joint(shin, thigh, new Vector3(side * .14f, .48f, 0), 70, 12);
                grips.Add(new Grip { Body = lowerArm, LocalPoint = new Vector3(0, -.16f, 0), Label = prefix + "hand" });
                grips.Add(new Grip { Body = shin, LocalPoint = new Vector3(0, -.18f, 0), Label = prefix + "ankle" });
            }

            // Append so the original hand/ankle grip indices remain stable.
            grips.Add(new Grip { Body = head, LocalPoint = Vector3.up * .13f, Label = "Head" });

            // Disable self-collision across connected limbs, while retaining world collision.
            for (int i = 0; i < colliders.Count; i++)
                for (int j = i + 1; j < colliders.Count; j++)
                    Physics.IgnoreCollision(colliders[i], colliders[j]);
            foreach (var part in parts)
            {
                restPositions.Add(part.transform.localPosition);
                restRotations.Add(part.transform.localRotation);
            }
            ResetAt(spawn);
        }

        Rigidbody Part(string label, Vector3 position, Vector3 size, float mass, Color color, bool sphere = false)
        {
            var obj = new GameObject(label);
            // Target/visibility rays check the environment, not the grabbed limb itself.
            obj.layer = 9;
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = position - Vector3.up;
            LabGeometry.Shape(obj.transform, "Visual", sphere ? PrimitiveType.Sphere : PrimitiveType.Cube,
                Vector3.zero, size, color, false);
            Collider collider;
            if (sphere) collider = obj.AddComponent<SphereCollider>();
            else collider = obj.AddComponent<BoxCollider>();
            if (collider is SphereCollider sphereCollider) sphereCollider.radius = size.x * .5f;
            if (collider is BoxCollider boxCollider) boxCollider.size = size;
            var body = obj.AddComponent<Rigidbody>();
            body.mass = mass;
            body.linearDamping = .3f;
            body.angularDamping = .6f;
            body.maxAngularVelocity = 12;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.solverIterations = 16;
            body.solverVelocityIterations = 8;
            parts.Add(body);
            colliders.Add(collider);
            return body;
        }

        void Joint(Rigidbody body, Rigidbody parent, Vector3 anatomicalAnchor, float swing, float twist)
        {
            var joint = body.gameObject.AddComponent<CharacterJoint>();
            joint.connectedBody = parent;
            var worldAnchor = transform.TransformPoint(anatomicalAnchor - Vector3.up);
            joint.anchor = body.transform.InverseTransformPoint(worldAnchor);
            joint.axis = Vector3.right;
            joint.swingAxis = Vector3.forward;
            joint.lowTwistLimit = new SoftJointLimit { limit = -twist };
            joint.highTwistLimit = new SoftJointLimit { limit = twist };
            joint.swing1Limit = new SoftJointLimit { limit = swing };
            joint.swing2Limit = new SoftJointLimit { limit = swing * .65f };
            joint.enableProjection = true;
            joint.projectionDistance = .08f;
            joint.projectionAngle = 15;
            joint.enablePreprocessing = true;
        }

        public void ResetAt(Vector3 spawn)
        {
            transform.SetPositionAndRotation(spawn, Quaternion.Euler(90, 0, 0));
            for (int i = 0; i < parts.Count; i++)
            {
                var body = parts[i];
                body.interpolation = RigidbodyInterpolation.None;
                var position = transform.TransformPoint(restPositions[i]);
                var rotation = transform.rotation * restRotations[i];
                body.transform.SetPositionAndRotation(position,rotation);
                body.position = position;
                body.rotation = rotation;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }
            Physics.SyncTransforms();
            foreach (var body in parts) body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public bool IsEntirelyIn(Bounds zone)
        {
            foreach (var collider in colliders)
                if (!zone.Contains(collider.bounds.min) || !zone.Contains(collider.bounds.max)) return false;
            return parts.Count > 0;
        }

        public bool IsFiniteAndContained()
        {
            foreach (var part in parts)
            {
                var p = part.position;
                if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) ||
                    float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z) ||
                    p.y < -5 || p.sqrMagnitude > 10000) return false;
            }
            return true;
        }
    }
}
