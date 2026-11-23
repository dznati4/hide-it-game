#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace HideIT
{
    public sealed class AsylumSmokeTest : MonoBehaviour
    {
        CarryLabBootstrap mission;
        string output;
        string routeFailure;

        IEnumerator Start()
        {
            mission = GetComponent<CarryLabBootstrap>();
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args,"-hideit-output");
            output = index >= 0 && index+1 < args.Length ? args[index+1] : Application.persistentDataPath;
            Directory.CreateDirectory(output);
            var atlas = Resources.Load<Texture2D>("AsylumKit/Textures/PSX_Asylum_Kit_atlas");
            var kit = Resources.Load<GameObject>("AsylumKit/Models/asylum_modular_wall_000");
            if (mission.BodySkin == null || kit == null || atlas == null || atlas.filterMode != FilterMode.Point ||
                mission.Guards.Length != 3 || Array.Exists(mission.Guards,guard => !guard.HasSightDisplay))
            { Fail("Asylum art, ragdoll, patrols or field-of-view meshes are missing."); yield break; }
            mission.SelectOutfit(0);
            mission.Begin();
            mission.Player.InputEnabled = false;

            var guard = mission.Guards[0];
            guard.transform.SetPositionAndRotation(new Vector3(0,.025f,7),Quaternion.LookRotation(Vector3.right));
            if (!guard.CanSee(new Vector3(2,1.1f,7)) || guard.CanSee(new Vector3(4.2f,1.1f,7)) ||
                guard.CanSee(new Vector3(-2,1.1f,7)))
            { Fail("Sight angles or tiled wall occlusion are wrong."); yield break; }

            mission.Retry();
            mission.Player.InputEnabled = false;
            mission.Player.ResetAt(new Vector3(.35f,.04f,11.65f));
            mission.Player.SetViewAngles(180,20);
            yield return new WaitForSeconds(.22f);
            if (!guard.CanSee(mission.Player.transform.position+Vector3.up*1.18f))
            { Fail("The guard did not see the unladen player in the cone."); yield break; }
            var preview = new GameObject("Patrol test view").AddComponent<Camera>();
            preview.CopyFrom(mission.Player.View);
            preview.depth++;
            preview.transform.position = new Vector3(-2.6f,2.45f,10.18f);
            preview.transform.LookAt(new Vector3(.18f,.95f,11.70f));
            yield return Capture("01-asylum-patrol.png");
            Destroy(preview.gameObject);
            yield return new WaitForSeconds(.64f);
            if (mission.Caught) { Fail("A guard caught a player who was not carrying the body."); yield break; }

            mission.Retry();
            mission.Player.InputEnabled = false;
            mission.Player.ResetAt(new Vector3(.35f,.04f,11.65f));
            mission.Player.SetViewAngles(0,18);
            mission.Body.ResetAt(new Vector3(.35f,.55f,12.25f));
            if (!mission.Player.Grab(mission.Body.Grips[0]))
            { Fail("Could not set up a carry inside the live guard cone."); yield break; }
            yield return new WaitForSecondsRealtime(1.15f);
            if (!mission.Caught || mission.Player.Holding || Time.timeScale != 0f)
            { Fail("A visible carry did not trigger the caught screen."); yield break; }
            yield return Capture("02-asylum-caught.png");

            mission.Retry();
            if (mission.Caught || mission.Delivered || Time.timeScale != 1f ||
                Vector3.Distance(mission.Player.transform.position,AsylumWard.PlayerSpawn) > .09f ||
                Vector3.Distance(mission.Body.Position,AsylumWard.BodySpawn) > .09f)
            { Fail($"Restart did not restore the mission: player={mission.Player.transform.position}, body={mission.Body.Position}, " +
                $"caught={mission.Caught}, delivered={mission.Delivered}, time scale={Time.timeScale}."); yield break; }
            mission.Pause(true);
            mission.ShowPauseOutfits(true);
            if (!mission.SelectOutfit(4) || mission.Player.OutfitId != CharacterArtCatalog.Players[4].Id || !mission.Paused)
            { Fail("Changing outfits in the pause menu failed."); yield break; }
            yield return Capture("03-asylum-character-change.png");
            mission.Pause(false);
            mission.Player.InputEnabled = false;
            yield return Capture("04-asylum-intake.png");
            mission.Player.ResetAt(new Vector3(.35f,.04f,-8.35f));
            mission.Player.SetViewAngles(0,20);
            if (!mission.Player.Grab(mission.Body.Grips[0]))
            { Fail("Could not grab the body at intake."); yield break; }
            var route = new[] {
                new Vector2(8.1f,-6.65f), new Vector2(8.05f,5.60f), new Vector2(11.05f,5.60f),
                new Vector2(11.10f,16.55f), new Vector2(0f,16.55f)
            };
            foreach (var target in route)
            {
                yield return WalkTo(target);
                if (routeFailure != null) { Fail(routeFailure); yield break; }
            }
            yield return new WaitForSecondsRealtime(1.3f);
            if (mission.Caught || !mission.Delivered)
            { Fail("Service route failed to deliver the body: " + mission.Body.Position); yield break; }
            mission.Player.SetViewAngles(-90,20);
            yield return Capture("05-asylum-exit.png");
            string result = "PASS: asylum art, sight cones, wall occlusion, safe empty-handed passage, caught and restart, pause outfit switch, and a playable service route to delivery.";
            File.WriteAllText(Path.Combine(output,"result.txt"),result);
            Debug.Log("HIDEIT_ASYLUM_SMOKE_PASS: "+result);
            Application.Quit(0);
        }

        IEnumerator WalkTo(Vector2 target)
        {
            var player = mission.Player;
            var controller = player.GetComponent<CharacterController>();
            float deadline = Time.realtimeSinceStartup+12;
            while (!mission.Caught && Time.realtimeSinceStartup < deadline)
            {
                var location = player.transform.position;
                var delta = new Vector3(target.x-location.x,0,target.y-location.z);
                if (delta.sqrMagnitude < .006f) yield break;
                player.SetViewAngles(Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,20);
                float frame = Time.deltaTime;
                controller.Move(Vector3.ClampMagnitude(delta,CarryLabPlayer.DragSpeed*frame)+Vector3.down*2f*frame);
                if (!player.Holding)
                { routeFailure = "Grip broke at " + location + " on the way to " + target; yield break; }
                yield return null;
            }
            routeFailure = $"Service route stopped before {target}; player={player.transform.position}, body={mission.Body.Position}, caught={mission.Caught}.";
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,name));
            yield return new WaitForSecondsRealtime(.23f);
        }

        void Fail(string reason)
        {
            File.WriteAllText(Path.Combine(output,"result.txt"),"FAIL: "+reason);
            Debug.LogError("HIDEIT_ASYLUM_SMOKE_FAIL: "+reason);
            Application.Quit(1);
        }
    }
}
#endif
