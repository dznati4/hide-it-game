#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace HideIT
{
    // Opt-in standalone acceptance check. It never runs during normal play.
    // The same CharacterController, spring joints, collision geometry, and delivery
    // condition are used; no transforms are teleported across the doorway.
    public sealed class CarryLabSmokeTest : MonoBehaviour
    {
        CarryLabBootstrap lab;
        string output;
        string gaitFailure;

        IEnumerator Start()
        {
            lab = GetComponent<CarryLabBootstrap>();
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-hideit-output");
            output = index >= 0 && index + 1 < args.Length ? args[index + 1] : Application.persistentDataPath;
            Directory.CreateDirectory(output);
            if (lab.BodySkin == null) { Fail("The exported ragdoll skin is missing."); yield break; }
            for (int i = 0; i < CharacterArtCatalog.Players.Length; i++)
            {
                if (!lab.SelectOutfit(i) || lab.Player.OutfitId != CharacterArtCatalog.Players[i].Id)
                { Fail("Cannot select outfit " + i); yield break; }
                yield return null;
            }
            lab.SelectOutfit(0);
            yield return new WaitForSeconds(.8f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "01-menu.png"));
            yield return new WaitForSeconds(.4f);
            lab.Begin();
            lab.Player.InputEnabled = false;
            yield return CheckLocomotion();
            if (gaitFailure != null) { Fail(gaitFailure); yield break; }
            lab.Player.ResetAt(new Vector3(0, .03f, -2.4f));
            yield return null;
            if (!lab.Player.TryFindGrip(out var startingGrip) || !lab.Player.Grab(startingGrip))
            { Fail("Could not select and grab a visible starting grip."); yield break; }

            yield return WalkTo(new Vector3(0, .03f, 6.1f), 0);
            if (!lab.Player.Holding) { Fail("Grip broke on the doorway route."); yield break; }
            if (lab.Body.Position.z < 3.2f) { Fail("Body remained behind the doorway: " + lab.Body.Position); yield break; }
            yield return new WaitForSeconds(.25f);
            yield return CaptureCarryPose("09-doorway-grip.png");
            if (!HandsOnGrip()) { Fail("The hands lost the grip after the doorway: " + GripDiagnostics()); yield break; }
            yield return WalkTo(new Vector3(4.1f, .03f, 6.1f), 90);
            // Give the trailing limbs time to enter the zone while the grip remains held.
            yield return new WaitForSeconds(2);
            if (!lab.Delivered || !lab.Player.Holding)
            { Fail("The level must complete as soon as the entire body enters, while still held."); yield break; }
            lab.Player.Release();
            yield return new WaitForSeconds(3);
            lab.Player.SetViewAngles(90, 28);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "02-delivery.png"));
            yield return new WaitForSeconds(.4f);
            if (!lab.Delivered) { Fail("Delivery did not complete: body=" + lab.Body.Position); yield break; }

            lab.Player.ToggleCamera();
            yield return new WaitForSeconds(.3f);
            if (!lab.Player.FirstPerson) { Fail("First-person switch failed."); yield break; }
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "03-first-person.png"));
            yield return new WaitForSeconds(.4f);
            lab.Player.ToggleCamera();
            lab.ResetLab();
            yield return new WaitForSeconds(.3f);
            if (lab.Delivered || lab.Player.Holding || lab.Player.FirstPerson || !lab.Body.IsFiniteAndContained())
            { Fail("Reset did not restore a playable state."); yield break; }
            foreach (string civilian in AmbientCivilians.ModelIds)
                if (CharacterArtCatalog.Load(civilian) == null)
                { Fail("Missing civilian: " + civilian); yield break; }
            float deadline = Time.realtimeSinceStartup + 15;
            while (lab.Civilians.CompletedPasses == 0 && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (lab.Civilians.CompletedPasses == 0)
            { Fail("No civilian crossed the room and exited."); yield break; }
            lab.Player.ResetAt(new Vector3(3.4f, .03f, 7.1f));
            lab.Player.SetViewAngles(0, 10);
            deadline = Time.realtimeSinceStartup + 18;
            while (Time.realtimeSinceStartup < deadline)
            {
                var position = lab.Civilians.LastWalkerPosition;
                if (lab.Civilians.ActiveCount > 0 && position.z > 9 && Mathf.Abs(position.x - 3.4f) < .65f) break;
                yield return null;
            }
            if (Time.realtimeSinceStartup >= deadline)
            { Fail("No civilian crossed the rear lane in view."); yield break; }
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "04-civilian.png"));
            yield return new WaitForSeconds(.3f);
            if (lab.BodySkin.MaxGripVisualError() > .02f)
            { Fail("Ragdoll skin no longer follows the physical grip positions."); yield break; }

            lab.Body.ResetAt(CarryLabRoom.BodySpawn);
            lab.Player.ResetAt(new Vector3(0, .03f, 1.2f));
            lab.Player.ToggleCamera();
            lab.Player.SetViewAngles(180, 40);
            if (!lab.Player.TryFindGrip(out var headGrip) || headGrip.Label != "Head" || !lab.Player.Grab(headGrip))
            { Fail("Could not grab the head on the updated body."); yield break; }
            var controller = lab.Player.GetComponent<CharacterController>();
            for (int step = 0; step < 45; step++)
            {
                controller.Move(new Vector3(0, -.1f, CarryLabPlayer.DragSpeed * Time.fixedDeltaTime));
                yield return new WaitForFixedUpdate();
                if (!lab.Player.Holding || Mathf.Abs(lab.BodySkin.CurrentNeckLength - lab.BodySkin.RestNeckLength) > .002f)
                { Fail("The head grip broke or the render neck changed length under load."); yield break; }
            }
            // Compare both poses after LateUpdate has synchronized the skin to interpolated physics.
            yield return new WaitForEndOfFrame();
            float headError = lab.BodySkin.HeadGripVisualError;
            float limbError = lab.BodySkin.MaxGripVisualError();
            if (headError > .065f || limbError > .02f)
            { Fail($"Visual grip alignment under load: head={headError}, limbs={limbError}"); yield break; }
            if (!HandsOnGrip() || !lab.Player.CarryArmsVisible)
            { Fail("First-person arms lost head contact: " + lab.Player.OutfitPose.PalmError); yield break; }
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "05-body-head-grip.png"));
            yield return new WaitForSeconds(.35f);
            lab.Player.ToggleCamera();
            yield return CaptureCarryPose("06-player-drag.png");
            float draggingHeight = headGrip.WorldPoint.y;
            lab.Player.SetLift(true);
            yield return new WaitForSeconds(.92f);
            if (!lab.Player.Holding || headGrip.WorldPoint.y < draggingHeight + .17f || !HandsOnGrip())
            { Fail($"Player lift lost contact: height={headGrip.WorldPoint.y}, drag height={draggingHeight}, palms={lab.Player.OutfitPose.PalmError}"); yield break; }
            yield return CaptureCarryPose("07-player-lift.png");
            lab.Player.SetCrouched(true);
            lab.Player.SetLift(false);
            yield return new WaitForSeconds(.72f);
            if (!lab.Player.Crouched || !HandsOnGrip() || headGrip.WorldPoint.y > draggingHeight + .05f)
            { Fail($"Crouched carry lost contact: height={headGrip.WorldPoint.y}, palms={lab.Player.OutfitPose.PalmError}"); yield break; }
            yield return CaptureCarryPose("08-player-crouch.png");
            lab.Player.Release();
            yield return new WaitForSeconds(.3f);
            if (lab.Player.OutfitPose.GripWeight > .001f)
            { Fail("Release did not blend back to the free pose."); yield break; }
            string result = "PASS: walk, run, crouch, doorway delivery, civilian walk, camera switch, fixed neck, hand contact through head drag/lift/crouch, and release blend.";
            File.WriteAllText(Path.Combine(output, "result.txt"), result);
            Debug.Log("HIDEIT_SMOKE_PASS: " + result);
            Application.Quit(0);
        }

        bool HandsOnGrip() => lab.Player.OutfitPose.GripWeight > .99f && lab.Player.OutfitPose.PalmError < .085f;

        string GripDiagnostics()
        {
            foreach (var grip in lab.Body.Grips)
                if (grip.Label == lab.Player.HeldLabel)
                    return $"{grip.Label} at {grip.WorldPoint}, player at {lab.Player.transform.position}, error {lab.Player.OutfitPose.PalmError:F3}";
            return "No grip held.";
        }

        IEnumerator CheckLocomotion()
        {
            var player = lab.Player;
            var controller = player.GetComponent<CharacterController>();
            var joints = player.OutfitPose.GetComponentsInChildren<Transform>();
            var hips = Array.Find(joints, joint => joint.name == "Hips");
            var foot = Array.Find(joints, joint => joint.name == "LeftFoot");
            var start = new Vector3(-2.5f,.03f,-5.9f);
            float walkLift = 0;
            foreach (float speed in new[] { CarryLabPlayer.WalkSpeed, CarryLabPlayer.RunSpeed })
            {
                player.ResetAt(start);
                yield return new WaitForEndOfFrame();
                float baseline = foot.position.y;
                float low = float.PositiveInfinity, high = float.NegativeInfinity, lift = 0;
                float finish = Time.time + .93f;
                while (Time.time < finish)
                {
                    controller.Move((Vector3.forward * speed + Vector3.down * 2f) * Time.deltaTime);
                    yield return new WaitForEndOfFrame();
                    float stride = foot.position.z - player.transform.position.z;
                    low = Mathf.Min(low,stride);
                    high = Mathf.Max(high,stride);
                    lift = Mathf.Max(lift,foot.position.y-baseline);
                    yield return null;
                }
                if (high-low < .48f || lift < .08f || (speed == CarryLabPlayer.RunSpeed && lift < walkLift + .06f))
                { gaitFailure = $"Walk/run foot cycle failed at {speed}: excursion={high-low}, lift={lift}"; yield break; }
                walkLift = lift;
            }
            player.ResetAt(start);
            yield return new WaitForEndOfFrame();
            float standing = hips.position.y;
            player.SetCrouched(true);
            yield return new WaitForSeconds(.27f);
            if (hips.position.y > standing - .28f)
                gaitFailure = "Crouch did not lower the hips with the feet on the floor.";
        }

        IEnumerator CaptureCarryPose(string file)
        {
            var view = new GameObject("Animation test camera").AddComponent<Camera>();
            view.CopyFrom(lab.Player.View);
            view.depth++;
            view.fieldOfView = 45;
            var player = lab.Player.transform.position;
            var forward = Quaternion.Euler(0,lab.Player.Yaw,0) * Vector3.forward;
            var side = Vector3.Cross(Vector3.up,forward);
            view.transform.position = player + side * 2.1f + forward * .9f + Vector3.up * 1.30f;
            view.transform.LookAt(player + forward * .46f + Vector3.up * .69f);
            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,file));
            yield return new WaitForSeconds(.2f);
            Destroy(view.gameObject);
        }

        IEnumerator WalkTo(Vector3 target, float yaw)
        {
            lab.Player.SetViewAngles(yaw, 15);
            var controller = lab.Player.GetComponent<CharacterController>();
            float timeout = Time.realtimeSinceStartup + 12;
            while (Vector2.Distance(new Vector2(lab.Player.transform.position.x, lab.Player.transform.position.z),
                       new Vector2(target.x, target.z)) > .07f && Time.realtimeSinceStartup < timeout)
            {
                var delta = target - lab.Player.transform.position;
                delta.y = 0;
                var step = Vector3.ClampMagnitude(delta, CarryLabPlayer.DragSpeed * Time.fixedDeltaTime);
                controller.Move(step + Vector3.down * .1f);
                yield return new WaitForFixedUpdate();
            }
        }

        void Fail(string message)
        {
            var details = new System.Text.StringBuilder();
            foreach (var collider in lab.Body.Colliders)
                details.AppendLine(collider.name + " " + collider.bounds);
            File.WriteAllText(Path.Combine(output, "result.txt"), "FAIL: " + message + "\n" + details);
            Debug.LogError("HIDEIT_SMOKE_FAIL: " + message);
            Application.Quit(1);
        }
    }
}
#endif
