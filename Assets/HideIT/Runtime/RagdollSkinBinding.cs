using System.Collections.Generic;
using UnityEngine;

namespace HideIT
{
    /// <summary>Calibrated visual binding. Physics, colliders, masses and grips remain authoritative.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class RagdollSkinBinding : MonoBehaviour
    {
        struct Link
        {
            public Transform Bone;
            public Rigidbody Physics;
            public Vector3 PositionOffset;
            public Quaternion RotationOffset;
        }
        struct Contact
        {
            public Transform Bone, Marker;
            public int GripIndex;
        }
        readonly List<Link> links = new List<Link>();
        readonly List<Contact> contacts = new List<Contact>();
        RagdollBody body;
        SkinnedMeshRenderer[] skins;
        Transform neck, head, headPhysics;
        Quaternion headRotationOffset;
        internal float RestNeckLength { get; private set; }
        internal float CurrentNeckLength => Vector3.Distance(neck.position, head.position);
        public GameObject Model { get; private set; }

        public static RagdollSkinBinding Attach(RagdollBody target)
        {
            var prefab = CharacterArtCatalog.Load("Body_Blue");
            if (prefab == null) return null;
            var originalRenderers = target.GetComponentsInChildren<Renderer>();
            var binding = target.gameObject.AddComponent<RagdollSkinBinding>();
            binding.body = target;
            binding.Model = Instantiate(prefab, target.transform);
            binding.Model.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
            CharacterArtCatalog.SetLayer(binding.Model.transform, 9);
            var transforms = new Dictionary<string, Transform>();
            foreach (var item in binding.Model.GetComponentsInChildren<Transform>(true))
            {
                string key = item.name.Contains("__") ? item.name.Substring(item.name.LastIndexOf("__", System.StringComparison.Ordinal) + 2) : item.name;
                if (!transforms.ContainsKey(key)) transforms.Add(key, item);
            }
            binding.Model.transform.position += target.Parts[0].position - transforms["Hips"].position;
            binding.neck = transforms["Neck"];
            binding.head = transforms["Head"];
            binding.headPhysics = target.Grips[4].Body.transform;
            binding.headRotationOffset = Quaternion.Inverse(binding.headPhysics.rotation) * binding.head.rotation;
            binding.RestNeckLength = binding.CurrentNeckLength;
            var mapping = new[] {
                "Hips", "Chest", "Head", "LeftUpperArm", "LeftLowerArm", "LeftUpperLeg", "LeftLowerLeg",
                "RightUpperArm", "RightLowerArm", "RightUpperLeg", "RightLowerLeg"
            };
            // Part order is recorded by RagdollBody.Build; the render skeleton has extra helper bones.
            for (int i = 0; i < mapping.Length; i++)
            {
                var physics = target.Parts[i];
                var bone = transforms[mapping[i]];
                if (bone == binding.head) continue;
                binding.links.Add(new Link {
                    Bone = bone, Physics = physics,
                    PositionOffset = Quaternion.Inverse(physics.rotation) * (bone.position - physics.position),
                    RotationOffset = Quaternion.Inverse(physics.rotation) * bone.rotation
                });
            }
            var boneNames = new[] { "LeftHand", "LeftFoot", "RightHand", "RightFoot", "Head" };
            var markerNames = new[] { "GripLeftHand", "GripLeftAnkle", "GripRightHand", "GripRightAnkle", "GripHead" };
            for (int i = 0; i < markerNames.Length; i++)
            {
                var marker = transforms[markerNames[i]];
                var bone = transforms[boneNames[i]];
                marker.SetParent(bone, true);
                binding.contacts.Add(new Contact { Bone = bone, Marker = marker, GripIndex = i });
            }
            binding.skins = binding.Model.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var skin in binding.skins) skin.updateWhenOffscreen = true;
            foreach (var renderer in originalRenderers) renderer.enabled = false;
            binding.ApplyPose();
            return binding;
        }

        void LateUpdate() { ApplyPose(); }
        public void ApplyPose()
        {
            if (body == null || skins == null) return;
            foreach (var link in links)
            {
                var physicsPose = link.Physics.transform;
                link.Bone.SetPositionAndRotation(physicsPose.TransformPoint(link.PositionOffset),
                    physicsPose.rotation * link.RotationOffset);
            }
            // Read rotation from physics. The head's position comes from the neck
            // hierarchy, so a spring pulling on the skull cannot stretch the neck.
            head.rotation = headPhysics.rotation * headRotationOffset;
            foreach (var contact in contacts)
                if (contact.Bone != head)
                    contact.Bone.position += body.Grips[contact.GripIndex].WorldPoint - contact.Marker.position;
            // Procedural poses can exceed imported rest bounds; keep bounds centered on actual physics.
            var world = body.Colliders[0].bounds;
            foreach (var collider in body.Colliders) world.Encapsulate(collider.bounds);
            world.Expand(.5f);
            foreach (var skin in skins)
            {
                var local = new Bounds(skin.transform.InverseTransformPoint(world.center), Vector3.zero);
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                            local.Encapsulate(skin.transform.InverseTransformPoint(world.center +
                                Vector3.Scale(world.extents, new Vector3(x, y, z))));
                skin.localBounds = local;
            }
        }

        public float MaxGripVisualError()
        {
            float result = 0;
            foreach (var contact in contacts)
                if (contact.Bone != head)
                    result = Mathf.Max(result, Vector3.Distance(contact.Marker.position, body.Grips[contact.GripIndex].WorldPoint));
            return result;
        }

        internal float HeadGripVisualError => Vector3.Distance(contacts[4].Marker.position, body.Grips[4].WorldPoint);

        internal Vector3 VisualGripPoint(RagdollBody.Grip grip)
        {
            foreach (var contact in contacts)
                if (body.Grips[contact.GripIndex].Body == grip.Body) return contact.Marker.position;
            return grip.WorldPoint;
        }
    }
}
