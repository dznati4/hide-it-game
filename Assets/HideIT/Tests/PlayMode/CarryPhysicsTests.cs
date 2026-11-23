using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HideIT.Tests
{
    public sealed class CarryPhysicsTests
    {
        GameObject root;
        RagdollBody body;
        CarryLabPlayer player;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Runtime auto-boot can also run when the test runner opens a scene.
            var existing = Object.FindFirstObjectByType<CarryLabBootstrap>();
            if (existing != null) Object.Destroy(existing.gameObject);
            yield return null;
            Time.timeScale = 1;
            root = new GameObject("Physics test fixture");
            var room = new GameObject("Room").AddComponent<CarryLabRoom>();
            room.transform.SetParent(root.transform);
            room.Build();
            body = new GameObject("Body").AddComponent<RagdollBody>();
            body.transform.SetParent(root.transform);
            body.Build(CarryLabRoom.BodySpawn);
            player = new GameObject("Player").AddComponent<CarryLabPlayer>();
            player.transform.SetParent(root.transform);
            player.Build(body);
            player.InputEnabled = false;
            yield return new WaitForFixedUpdate();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            yield return null;
            LabGeometry.ClearMaterialCache();
            Time.timeScale = 1;
        }

        [UnityTest]
        public IEnumerator DroppedBodyStaysAboveFloorAndConnected()
        {
            for (int i = 0; i < 180; i++) yield return new WaitForFixedUpdate();
            Assert.That(body.IsFiniteAndContained(), Is.True, "Ragdoll fell out of the room or became invalid.");
            foreach (var collider in body.Colliders)
                Assert.That(collider.bounds.min.y, Is.GreaterThan(-.12f), collider.name + " tunneled through the floor.");
            foreach (var joint in body.GetComponentsInChildren<CharacterJoint>())
            {
                var a = joint.transform.TransformPoint(joint.anchor);
                var b = joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);
                Assert.That(Vector3.Distance(a, b), Is.LessThan(.25f), joint.name + " separated at its joint.");
            }
        }

        [UnityTest]
        public IEnumerator PlayerCanPullBodyThroughDoorway()
        {
            for (int i = 0; i < 90; i++) yield return new WaitForFixedUpdate();
            player.ResetAt(new Vector3(0, .03f, -2.4f));
            Assert.That(player.Grab(body.Grips[0]), Is.True, "Starting grip was unreachable.");
            var controller = player.GetComponent<CharacterController>();
            for (int i = 0; i < 360; i++)
            {
                controller.Move(new Vector3(0, -.1f, CarryLabPlayer.DragSpeed * Time.fixedDeltaTime));
                yield return new WaitForFixedUpdate();
            }
            Assert.That(player.Holding, Is.True, "Grip broke during ordinary dragging.");
            Assert.That(body.Position.z, Is.GreaterThan(3.2f), "Body did not pass the doorway.");
            Assert.That(body.IsFiniteAndContained(), Is.True);
            player.Release();
            yield return null;
            Assert.That(body.GetComponentsInChildren<SpringJoint>(), Is.Empty, "Release left a force-producing joint behind.");
        }

        [UnityTest]
        public IEnumerator ResetRemovesGripAndRestoresEveryLimb()
        {
            player.ResetAt(new Vector3(0, .03f, -2.4f));
            Assert.That(player.Grab(body.Grips[0]), Is.True);
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            player.ResetAt(CarryLabRoom.PlayerSpawn);
            body.ResetAt(CarryLabRoom.BodySpawn);
            yield return null;
            Assert.That(player.Holding, Is.False);
            Assert.That(body.GetComponentsInChildren<SpringJoint>(), Is.Empty);
            Assert.That(Vector3.Distance(body.Position, CarryLabRoom.BodySpawn), Is.LessThan(.1f));
            Assert.That(body.IsFiniteAndContained(), Is.True);
        }

        [UnityTest]
        public IEnumerator VisibleGripCanBeTargetedAndGrabbed()
        {
            for (int i = 0; i < 90; i++) yield return new WaitForFixedUpdate();
            player.ResetAt(new Vector3(0, .03f, -2.4f));
            yield return null;
            Assert.That(player.TryFindGrip(out var grip), Is.True, "A visible limb blocked its own targeting ray.");
            Assert.That(player.Grab(grip), Is.True);
            Assert.That(player.Holding, Is.True);
        }

        [UnityTest]
        public IEnumerator OriginalLimbGripsRemainAvailableInTheirExistingOrder()
        {
            var labels = new[] { "Left hand", "Left ankle", "Right hand", "Right ankle" };
            Assert.That(body.Grips.Count, Is.EqualTo(5), "Head must extend the existing grip set.");
            for (int i = 0; i < labels.Length; i++)
            {
                body.ResetAt(CarryLabRoom.BodySpawn);
                player.ResetAt(new Vector3(0, .03f, -2.4f));
                var grip = body.Grips[i];
                Assert.That(grip.Label, Is.EqualTo(labels[i]), "An existing grip index changed.");
                Assert.That(player.Grab(grip), Is.True, labels[i] + " is no longer grabbable.");
                Assert.That(player.HeldLabel, Is.EqualTo(labels[i]));
                Assert.That(grip.Body.GetComponent<SpringJoint>(), Is.Not.Null);
                player.Release();
                yield return null;
                Assert.That(body.GetComponentsInChildren<SpringJoint>(), Is.Empty);
            }
        }

        [UnityTest]
        public IEnumerator HeadCanBeTargetedPulledReleasedAndReset()
        {
            yield return PrepareHeadTargeting();
            Assert.That(player.TryFindGrip(out var grip), Is.True, "Normal targeting could not find the visible head.");
            Assert.That(grip.Label, Is.EqualTo("Head"));
            Assert.That(grip.Body, Is.SameAs(body.Parts[2]), "Head grip must pull the existing head rigidbody.");
            var skull = grip.Body.GetComponent<SphereCollider>();
            Assert.That(grip.LocalPoint, Is.EqualTo(Vector3.up * skull.radius), "Grip should attach at the top of the skull.");
            var start = grip.Body.position;
            Assert.That(player.Grab(grip), Is.True);
            Assert.That(player.HeldLabel, Is.EqualTo("Head"));
            var joint = grip.Body.GetComponent<SpringJoint>();
            Assert.That(joint, Is.Not.Null);
            Assert.That(joint.anchor, Is.EqualTo(grip.LocalPoint));

            // Face the head and walk backwards, using the same controller and
            // spring as gameplay instead of teleporting the body into position.
            var controller = player.GetComponent<CharacterController>();
            for (int i = 0; i < 45; i++)
            {
                controller.Move(new Vector3(0, -.1f, CarryLabPlayer.DragSpeed * Time.fixedDeltaTime));
                yield return new WaitForFixedUpdate();
            }
            Assert.That(player.Holding, Is.True, "Head grip broke during an ordinary pull.");
            Assert.That(grip.Body.position.z - start.z, Is.GreaterThan(.3f), "The head did not follow the pull.");
            Assert.That(body.IsFiniteAndContained(), Is.True);
            player.Release();
            Assert.That(player.Holding, Is.False);
            Assert.That(joint.spring, Is.Zero, "Release must stop pulling before deferred joint destruction.");
            Assert.That(joint.damper, Is.Zero);
            yield return null;
            Assert.That(body.GetComponentsInChildren<SpringJoint>(), Is.Empty);

            yield return PrepareHeadTargeting();
            Assert.That(player.TryFindGrip(out var resetGrip), Is.True);
            Assert.That(resetGrip.Label, Is.EqualTo("Head"));
            Assert.That(player.Grab(resetGrip), Is.True);
            player.ResetAt(CarryLabRoom.PlayerSpawn);
            body.ResetAt(CarryLabRoom.BodySpawn);
            Assert.That(player.Holding, Is.False, "Reset left the head held.");
            Assert.That(player.HeldLabel, Is.Empty);
            Assert.That(Vector3.Distance(body.Position, CarryLabRoom.BodySpawn), Is.LessThan(.001f));
            foreach (var part in body.Parts)
            {
                Assert.That(part.linearVelocity.sqrMagnitude, Is.LessThan(.0001f), part.name + " retained reset velocity.");
                Assert.That(part.angularVelocity.sqrMagnitude, Is.LessThan(.0001f), part.name + " retained reset spin.");
            }
            yield return null;
            Assert.That(body.GetComponentsInChildren<SpringJoint>(), Is.Empty, "Reset left a head attachment behind.");
            Assert.That(body.IsFiniteAndContained(), Is.True);
        }

        [UnityTest]
        public IEnumerator HeadTargetingRespectsWorldOcclusion()
        {
            yield return PrepareHeadTargeting();
            Assert.That(player.TryFindGrip(out var visible), Is.True);
            Assert.That(visible.Label, Is.EqualTo("Head"), "Establish a target before adding an obstruction.");
            var eye = player.transform.position + Vector3.up * .9f;
            var blocker = new GameObject("Head line-of-sight blocker");
            blocker.transform.SetParent(root.transform);
            blocker.transform.position = (eye + visible.WorldPoint) * .5f;
            blocker.AddComponent<BoxCollider>().size = new Vector3(.8f, 1.2f, .2f);
            Physics.SyncTransforms();
            Assert.That(Physics.Linecast(eye, visible.WorldPoint, 1 << 0, QueryTriggerInteraction.Ignore), Is.True);
            Assert.That(player.TryFindGrip(out _), Is.False, "Head targeting ignored world collision.");

            Object.Destroy(blocker);
            yield return null;
            Physics.SyncTransforms();
            Assert.That(player.TryFindGrip(out var restored), Is.True, "Removing the obstruction should restore targeting.");
            Assert.That(restored.Label, Is.EqualTo("Head"));
        }

        IEnumerator PrepareHeadTargeting()
        {
            body.ResetAt(CarryLabRoom.BodySpawn);
            // The head is nearest from this end; hands and ankles are outside
            // GrabRange. Use public camera controls, not a test-only camera pose.
            player.ResetAt(new Vector3(0, .03f, 1.2f));
            if (!player.FirstPerson) player.ToggleCamera();
            player.SetViewAngles(180, 35);
            yield return null;
            var eye = player.transform.position + Vector3.up * .9f;
            Assert.That(Vector3.Distance(eye, body.Grips[4].WorldPoint), Is.LessThan(CarryLabPlayer.GrabRange));
            for (int i = 0; i < 4; i++)
                Assert.That(Vector3.Distance(eye, body.Grips[i].WorldPoint), Is.GreaterThan(CarryLabPlayer.GrabRange));
        }

        [UnityTest]
        public IEnumerator BodyCannotBeGrabbedAcrossThePartition()
        {
            // Keep every limb behind the wall; an ankle protruding onto the
            // player's side is legitimately reachable and must not fail this check.
            body.ResetAt(new Vector3(3, .65f, 4.4f));
            player.ResetAt(new Vector3(3, .03f, 2.4f));
            yield return new WaitForFixedUpdate();
            Assert.That(player.TryFindGrip(out _), Is.False, "Grip targeting ignored a wall.");
        }

        [UnityTest]
        public IEnumerator LevelCompletesWhenEntireBodyEntersWhilePlayerStaysOutsideAndHoldsIt()
        {
            root.SetActive(false);
            var legacy = new GameObject("Delivery test");
            legacy.SetActive(false);
            var lab = legacy.AddComponent<CarryLabBootstrap>();
            lab.LegacyMode = true;
            legacy.SetActive(true);
            var zone = lab.DropZone;
            try
            {
                lab.Begin();
                lab.Player.InputEnabled = false;
                lab.Body.ResetAt(new Vector3(zone.center.x, .65f, zone.max.z - .05f));
                yield return null;
                Assert.That(lab.Delivered, Is.False, "A protruding head must prevent completion.");

                lab.Player.ResetAt(new Vector3(zone.min.x - .12f, .03f, zone.center.z));
                lab.Body.ResetAt(new Vector3(zone.center.x, .65f, zone.center.z));
                Assert.That(lab.Player.Grab(lab.Body.Grips[0]), Is.True);
                foreach (var part in lab.Body.Parts) part.linearVelocity = Vector3.forward;
                yield return null;
                Assert.That(zone.Contains(lab.Player.transform.position), Is.False);
                Assert.That(lab.Player.Holding, Is.True);
                Assert.That(lab.Delivered, Is.True, "Completion should not require releasing, standing inside, or waiting for the body to settle.");

                lab.Body.ResetAt(CarryLabRoom.BodySpawn);
                yield return null;
                Assert.That(lab.Delivered, Is.True, "Completion stays visible until retry.");
                lab.ResetLab();
                Assert.That(lab.Delivered, Is.False);
            }
            finally { Object.Destroy(lab.gameObject); }
        }
    }
}
