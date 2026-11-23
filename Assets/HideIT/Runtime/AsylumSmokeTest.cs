#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
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
            { Fail("Asylum assets, ragdoll or guard sight displays are missing."); yield break; }

            mission.SelectOutfit(0);
            yield return Capture("01-night-shift-menu.png");
            mission.ShowGuide(true);
            yield return Capture("02-controls-guide.png");
            mission.ShowGuide(false);
            mission.Begin();
            mission.Player.InputEnabled = false;
            yield return Capture("03-minimal-hud.png");

            var guard = mission.Guards[0];
            guard.transform.SetPositionAndRotation(new Vector3(0,.025f,7),Quaternion.LookRotation(Vector3.right));
            if (!guard.CanSee(new Vector3(2,1.1f,7)) || guard.CanSee(new Vector3(4.2f,1.1f,7)) ||
                guard.CanSee(new Vector3(-2,1.1f,7)))
            { Fail("Vision did not respect walls and field of view."); yield break; }
            var route = new List<Vector3>();
            var adjacentWard = new Vector3(5.5f,.025f,7.75f);
            if (!mission.Navigation.TryFindPath(new Vector3(0,.025f,7),adjacentWard,route) ||
                route.Count < 2 || Vector3.Distance(route[route.Count-1],adjacentWard) > .36f)
            { Fail("Guard navigation could not route through an adjacent doorway. " + string.Join(" -> ",route)); yield break; }
            foreach (var destination in new[] { new Vector3(-8,.025f,12),new Vector3(10.8f,.025f,12),new Vector3(0,.025f,14.85f) })
                if (!mission.Navigation.TryFindPath(AsylumWard.PlayerSpawn,destination,route) ||
                    Vector3.Distance(route[route.Count-1],destination) > .42f)
                { Fail("The level has an inaccessible passage at " + destination); yield break; }

            Rewind();
            mission.Player.ResetAt(new Vector3(.35f,.045f,12.66f));
            mission.Player.SetViewAngles(180,10);
            yield return new WaitForSecondsRealtime(.21f);
            if (guard.State != AsylumGuard.Activity.Chase || mission.Caught)
            { Fail("A visible player should be chased before being caught: " + guard.State); yield break; }
            yield return new WaitForSecondsRealtime(1.03f);
            if (!mission.Caught || Time.timeScale != 0 || mission.Player.Holding)
            { Fail("Close pursuit did not trigger the caught screen."); yield break; }
            yield return Capture("04-guard-capture.png");

            Rewind();
            if (mission.Caught || mission.Delivered || mission.Interactions.Rocks != 3 || Time.timeScale != 1f ||
                Vector3.Distance(mission.Player.transform.position,AsylumWard.PlayerSpawn) > .09f ||
                Vector3.Distance(mission.Body.Position,AsylumWard.BodySpawn) > .09f)
            { Fail("Retry did not restore the player, body and rocks."); yield break; }
            mission.Pause(true);
            yield return Capture("05-pause-menu.png");
            mission.ShowGuide(true);
            yield return Capture("06-pause-controls.png");
            mission.ShowGuide(false);
            mission.ShowPauseOutfits(true);
            if (!mission.SelectOutfit(4) || mission.Player.OutfitId != CharacterArtCatalog.Players[4].Id ||
                !mission.Paused || Vector3.Distance(mission.Player.transform.position,AsylumWard.PlayerSpawn) > .09f)
            { Fail("Pause outfit change altered mission state or failed."); yield break; }
            yield return Capture("07-pause-roster.png");
            mission.Pause(false);
            mission.Player.InputEnabled = false;

            Rewind();
            mission.Player.ResetAt(new Vector3(.35f,.045f,12.4f));
            yield return new WaitForSecondsRealtime(.13f);
            if (guard.State != AsylumGuard.Activity.Chase)
            { Fail("Could not start the hidden escape from an active chase."); yield break; }
            mission.Player.ResetAt(new Vector3(-5.20f,.04f,2.77f));
            if (!mission.Interactions.Interact() || !mission.Interactions.Hidden || mission.Caught)
            { Fail("Hiding after breaking sight failed."); yield break; }
            yield return Capture("08-hidden.png");
            yield return new WaitForSecondsRealtime(5.05f);
            if (mission.Caught || !mission.Interactions.Hidden || guard.State == AsylumGuard.Activity.Chase ||
                guard.State == AsylumGuard.Activity.Search)
            { Fail("The guard failed to give up after losing sight of the hidden player."); yield break; }
            mission.Interactions.Interact();
            if (mission.Interactions.Hidden) { Fail("Exit from hiding failed."); yield break; }

            Rewind();
            guard.transform.SetPositionAndRotation(new Vector3(-1.85f,.025f,-8.01f),Quaternion.identity);
            mission.Player.ResetAt(new Vector3(-1.85f,.04f,-5.77f));
            if (!guard.CanSee(mission.Player.SightPoint) || !mission.Interactions.Interact() || !mission.Caught)
            { Fail("A witnessed hiding attempt must trigger capture."); yield break; }

            Rewind();
            mission.Body.ResetAt(new Vector3(.35f,.45f,11.79f));
            mission.Player.ResetAt(new Vector3(.35f,.04f,8.48f));
            mission.Player.SetViewAngles(0,14);
            yield return new WaitForSecondsRealtime(.75f);
            if (guard.State != AsylumGuard.Activity.InspectBody || mission.Caught)
            { Fail("The ground body did not draw the guard: " + guard.State); yield break; }
            var beforeRock = guard.transform.position;
            if (!mission.Interactions.ThrowRock(Vector3.right) || mission.Interactions.Rocks != 2)
            { Fail("Throwing a rock did not use a charge."); yield break; }
            yield return new WaitForSecondsRealtime(.65f);
            if (guard.State != AsylumGuard.Activity.InvestigateNoise || guard.DistractionSeconds <= 0 || mission.Caught)
            { Fail("The rock impact did not interrupt body inspection: " + guard.State); yield break; }
            yield return Capture("09-noise-investigation.png");
            yield return new WaitForSecondsRealtime(1.30f);
            if (Vector3.Distance(beforeRock,guard.transform.position) < .45f || guard.State != AsylumGuard.Activity.InvestigateNoise)
            { Fail("The guard did not walk to the noise or left too soon."); yield break; }

            Rewind();
            guard.transform.rotation = Quaternion.Euler(0,180,0);
            mission.Player.ResetAt(new Vector3(.15f,.045f,14.94f));
            mission.Player.SetViewAngles(0,18);
            mission.Body.ResetAt(new Vector3(.16f,.42f,15.28f));
            yield return new WaitForSecondsRealtime(.14f);
            if (mission.Delivered) { Fail("Simply leaving the body near the dumpster completed the mission."); yield break; }
            if (!mission.Player.Grab(mission.Body.Grips[0]) || !mission.Interactions.Interact() || !mission.Interactions.Stashing)
            { Fail("The dumpster interaction did not accept a carried body."); yield break; }
            yield return Capture("10-dumpster-stash.png");
            yield return new WaitForSecondsRealtime(.75f);
            if (!mission.Delivered || mission.Caught || mission.Body.gameObject.activeSelf)
            { Fail("Stashing did not close the dumpster and complete the mission."); yield break; }
            yield return Capture("11-mission-complete.png");
            Rewind();
            if (!mission.Body.gameObject.activeSelf || mission.Interactions.Stashing || mission.Delivered)
            { Fail("Restart after a successful stash did not restore the mission."); yield break; }

            yield return RunServiceRoute();
            if (routeFailure != null) { Fail(routeFailure); yield break; }
            yield return Capture("12-service-route-complete.png");

            string result = "PASS: menus and guides, wall occlusion, connected patrol routes, chase and capture, witnessed and unseen hiding, body investigation, physical rock distraction, dumpster victory, restart and a complete route with live guards.";
            File.WriteAllText(Path.Combine(output,"result.txt"),result);
            Debug.Log("HIDEIT_ASYLUM_SMOKE_PASS: "+result);
            Application.Quit(0);
        }

        IEnumerator RunServiceRoute()
        {
            var player = mission.Player;
            var controller = player.GetComponent<CharacterController>();
            player.ResetAt(new Vector3(.35f,.04f,-8.35f));
            if (!player.Grab(mission.Body.Grips[0])) { routeFailure = "Route: could not pick up the body at intake."; yield break; }
            var targets = new[] {
                new Vector2(8.1f,-6.65f),new Vector2(8.05f,5.6f),new Vector2(11.05f,5.6f),new Vector2(11.06f,8.05f),
                new Vector2(11.11f,16.52f),new Vector2(3.15f,16.52f),new Vector2(2.45f,15.01f),new Vector2(.3f,15.01f)
            };
            bool sawWidePatrol = false;
            for (int i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                float deadline = Time.realtimeSinceStartup+11;
                while (!mission.Caught && player.Holding && Time.realtimeSinceStartup < deadline)
                {
                    var p = player.transform.position;
                    var remaining = new Vector3(target.x-p.x,0,target.y-p.z);
                    if (remaining.sqrMagnitude < .0064f) break;
                    player.SetViewAngles(Mathf.Atan2(remaining.x,remaining.z)*Mathf.Rad2Deg,15);
                    float frame = Time.deltaTime;
                    controller.Move(Vector3.ClampMagnitude(remaining,CarryLabPlayer.DragSpeed*frame)+Vector3.down*2f*frame);
                    sawWidePatrol |= mission.Guards[0].transform.position.z > 14.4f;
                    yield return null;
                }
                var at = player.transform.position;
                if (mission.Caught || !player.Holding || Vector2.Distance(new Vector2(at.x,at.z),target) > .13f)
                {
                    routeFailure = $"Live route stopped at leg {i+1}, pos={at}, body={mission.Body.Position}, " +
                        $"states={mission.Guards[0].State}/{mission.Guards[1].State}/{mission.Guards[2].State}, " +
                        $"east guard={mission.Guards[2].transform.position}, caught={mission.Caught}.";
                    yield break;
                }
                if (i == 3)
                {
                    mission.Interactions.ThrowRock(new Vector3(-.085f,0,1));
                    yield return new WaitForSecondsRealtime(.73f);
                }
            }
            if (!sawWidePatrol || !mission.Interactions.Interact() || !mission.Interactions.Stashing)
            { routeFailure = "Live route did not pass a wider patrol and stash the carried body."; yield break; }
            yield return new WaitForSecondsRealtime(.82f);
            if (!mission.Delivered || mission.Caught) routeFailure = "Live route did not finish at the dumpster.";
        }

        void Rewind()
        {
            mission.Retry();
            mission.Player.InputEnabled = false;
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,name));
            yield return new WaitForSecondsRealtime(.18f);
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
