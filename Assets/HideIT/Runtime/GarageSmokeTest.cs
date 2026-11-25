#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HideIT
{
    public sealed class GarageSmokeTest : MonoBehaviour
    {
        IEnumerator Start()
        {
            var mission = GetComponent<CarryLabBootstrap>();
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args,"-hideit-output");
            var output = index >= 0 && index+1 < args.Length ? args[index+1] : Application.persistentDataPath;
            Directory.CreateDirectory(output);
            void End(string detail,bool success = false)
            {
                File.WriteAllText(Path.Combine(output,"result.txt"),(success ? "PASS: " : "FAIL: ")+detail);
                if (success) Debug.Log(detail);
                else Debug.LogError(detail);
                Application.Quit(success ? 0 : 1);
            }

            // Exercise both choices through the same scene and title flow.
            mission.SelectStage(HideItStage.Asylum);
            mission.Begin();
            if (mission.CurrentStage != HideItStage.Asylum || mission.GetComponentInChildren<ParkingGarage>() != null)
            { End("Selecting Asylum did not replace the active environment."); yield break; }
            mission.ReturnToMenu();
            mission.SelectStage(HideItStage.Garage);
            yield return Capture(output,"01-garage-selection.png");
            mission.Begin();
            var garage = mission.GetComponentInChildren<ParkingGarage>();
            var parking = mission.Guards;
            bool HasTexture(string name)
            {
                var prop = garage.transform.Find(name);
                return prop != null && Array.Exists(prop.GetComponentsInChildren<MeshRenderer>(), r =>
                    Array.Exists(r.sharedMaterials, m => m != null && m.mainTexture is Texture2D texture &&
                        texture.filterMode == FilterMode.Point));
            }
            if (mission.CurrentStage != HideItStage.Garage || garage == null || garage.ImportedProps < 12 ||
                !HasTexture("PSX_Commercial_PizzaRestaurant") || !HasTexture("SM_Bulkhead_Door") ||
                parking.Length != 3 || Array.Exists(parking,p => !p.HasSightDisplay) ||
                mission.GetComponentInChildren<AsylumWard>() != null)
            { End("Switching stages, textured sample art, or garage patrols failed."); yield break; }

            mission.Player.InputEnabled = false;
            yield return Capture(output,"02-street-intake.png");
            var player = mission.Player;
            player.SetViewAngles(135,5);
            yield return Capture(output,"02b-city-frontage.png");
            var route = new List<Vector3>();
            foreach (var destination in new[] { new Vector3(-7.7f,.025f,14.7f),new Vector3(8f,.025f,13.3f) })
                if (!mission.Navigation.TryFindPath(ParkingGarage.PlayerSpawn,destination,route) ||
                    (route[route.Count-1]-destination).sqrMagnitude > .2f)
                { End("A garage route is blocked at "+destination); yield break; }

            parking[0].transform.SetPositionAndRotation(new Vector3(-8.15f,.025f,-1.97f),Quaternion.LookRotation(Vector3.right));
            if (!parking[0].CanSee(new Vector3(-6.55f,1.15f,-1.97f)) ||
                parking[0].CanSee(new Vector3(-4.2f,1.15f,-1.97f)))
            { End("Parked cars must obstruct guard sight."); yield break; }

            mission.Retry();
            player.InputEnabled = false;
            player.ResetAt(new Vector3(-9.98f,.045f,4.65f));
            if (mission.Interactions.Prompt != "[ E ] Hide in locker" ||
                !mission.Interactions.Interact() || !mission.Interactions.Hidden || mission.Caught)
            { End("The west garage locker cannot be used from the lane."); yield break; }
            mission.Interactions.Interact();
            if (mission.Interactions.Hidden)
            { End("The player could not leave the garage locker."); yield break; }

            player.ResetAt(new Vector3(8.4f,.045f,13.32f));
            player.SetViewAngles(14,2);
            yield return Capture(output,"03-maintenance.png");
            mission.Retry();
            player.InputEnabled = false;
            if (!player.Grab(mission.Body.Grips[0]))
            { End("The body is out of reach at the garage spawn."); yield break; }
            var controller = player.GetComponent<CharacterController>();
            var targets = new[] {
                new Vector2(-8.17f,-2.8f),new Vector2(-8.38f,4.7f),new Vector2(-8.38f,10.91f),
                new Vector2(-10.2f,11.53f),new Vector2(-10.2f,13.45f),new Vector2(-7.62f,14.83f)
            };
            for (int i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                float deadline = Time.realtimeSinceStartup+12;
                while (!mission.Caught && player.Holding && Time.realtimeSinceStartup < deadline)
                {
                    var p = player.transform.position;
                    var step = new Vector3(target.x-p.x,0,target.y-p.z);
                    if (step.sqrMagnitude < .008f) break;
                    player.SetViewAngles(Mathf.Atan2(step.x,step.z)*Mathf.Rad2Deg,10);
                    float dt = Time.deltaTime;
                    controller.Move(Vector3.ClampMagnitude(step,CarryLabPlayer.DragSpeed*dt)+Vector3.down*2f*dt);
                    yield return null;
                }
                var at = player.transform.position;
                if (mission.Caught || !player.Holding || Vector2.Distance(new Vector2(at.x,at.z),target) > .13f)
                {
                    End($"Garage route stopped at leg {i+1}, player {at}, guard {parking[0].transform.position}, " +
                        $"states {parking[0].State}/{parking[1].State}/{parking[2].State}.");
                    yield break;
                }
                if (i == 0)
                {
                    mission.Interactions.ThrowRock(new Vector3(1,-.2f,.10f));
                    yield return Capture(output,"04-parking.png");
                }
                if (i == 1) mission.Interactions.ThrowRock(new Vector3(1,-.3f,-.04f));
            }
            if (mission.Delivered || !mission.Interactions.Interact() || !mission.Interactions.Stashing)
            { End("The garage dumpster must require a stash interaction."); yield break; }
            yield return new WaitForSecondsRealtime(.77f);
            if (!mission.Delivered || mission.Caught)
            { End("The garage dumpster did not complete the level."); yield break; }
            yield return Capture(output,"05-garage-complete.png");
            mission.ReturnToMenu();
            mission.Begin();
            if (!mission.Body.gameObject.activeSelf || mission.Delivered || mission.Interactions.Rocks != 3)
            { End("Restarting from the title did not reset the garage."); yield break; }
            End("Asylum and garage selection, point-filtered assets from both packs, car occlusion, usable lockers, " +
                "connected routes, garage delivery with live patrols, and replay.",true);
        }

        static IEnumerator Capture(string output,string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,name));
            yield return new WaitForSecondsRealtime(.18f);
        }
    }
}
#endif
