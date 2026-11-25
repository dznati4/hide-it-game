# Hide iT

**ZanettiZONEai** · Unity 6.6 · Windows · work in progress

Build the mechanic, verify it, then move to the next system. Art comes later.

## Current playtest

Pick one of five masked characters and stash the body in the service dumpster.
Choose **Asylum** for the ward and its side corridors, or **Garage** for a lower-level parking
deck with parked cars, a security booth, and a maintenance passage. Both missions cross patrol
areas before the loading bay. Use **Main Menu** from a pause or result screen to switch maps.

The floor cones show where guards can see. When a guard spots you, the cone turns red and the
guard gives chase. Break line of sight before hiding in a closet or under a bed. Guards who see
you enter a hiding spot can catch you there. A guard who spots the unattended body investigates
it. Throw a rock to draw nearby guards to the impact for a few seconds.
The roster comes from [Characters PSX by Elbolilloduro](https://elbolilloduro.itch.io/characters-psx).
The ward uses the free [PSX Asylum Kit by Harning](https://harning.itch.io/psx-asylum-kit).

See the [current export status](EXPORT_STATUS.md) for build and test results.

To play the built prototype directly, double-click **Play Hide iT.cmd**.
Keep the executable together with all adjacent files in `Builds/Playtest-0.0.10`.

## Open and play

1. Install **Unity 6000.6.10f1** with Windows support through Unity Hub.
2. Sign in and activate your own Unity license. No paid assets are required.
3. Add this `hide-it` directory as an existing project in Hub, or double-click `Open Hide iT.cmd`.
4. If the local art files are missing, follow the import steps below. Choose **Hide iT → Open Playtest**, then press **Play**.
5. Choose a character and a map, then press Enter or click **Start Mission**.

The initial scene is included in the project. The room is generated when Play starts,
so only a marker in the scene's Hierarchy in Edit mode is expected.

| Control | Action |
| --- | --- |
| WASD / mouse | Move / look |
| Left Shift | Run when not dragging |
| Left Ctrl or C | Crouch |
| E | Use the action shown on screen. Grab or release a limb, hide or leave cover, pick up rocks, or stash the body. |
| Q | Drop |
| Hold left mouse | Raise the held limb |
| G | Throw a rock in the direction you are looking |
| V | Switch first / third person |
| R | Restart the mission |
| Esc | Pause / resume. Open **Controls** or **Change Character** from the pause menu. |

You start with three rocks. Aim at where you want them to land and look for refills around the ward.
Carry the body to the front of the dumpster and press **E** when **Stash body** appears.
**LEVEL COMPLETE** appears when the lid closes. Use **R**, **Enter**, or **Restart** to play again.

## Prepare the local character assets

Download the free `Characters_psx_1.2.zip` from the [creator's page](https://elbolilloduro.itch.io/characters-psx).
Keep the archive intact. In the project folder, run this using Blender's executable or its full path.

```powershell
blender --background --factory-startup --python-exit-code 1 --python art/blender/import_psx_characters.py -- --pack 'C:\path\Characters_psx_1.2.zip'
.\scripts\unity.ps1 -Action ImportArt
```

The pack's license permits game use and prohibits re-uploading the assets, including modified copies.
The local model and texture folder is omitted from source archives. The import saves an editable local
ragdoll in `.tools/CharactersPSX/Hide-iT-PSX-Body.blend`. The selected models are listed in
`art/psx-character-manifest.json`.

Download `PSX_Asylum_Kit_Free_v1.0.zip` from Harning's page and run the importer before building.

```powershell
.\scripts\import-asylum-kit.ps1 -Archive 'C:\path\PSX_Asylum_Kit_Free_v1.0.zip'
```

The kit lives in a local-only Unity Resources folder under its game-use license.

For the garage, get the free City and Submarine samples from Zenthrall Games and keep the ZIPs intact.
[City sample](https://zenthrall.itch.io/ultimate-psx-city-pack-free-sample)
[Submarine sample](https://zenthrall.itch.io/psx-submarine-kit-free-sample)

```powershell
blender --background --factory-startup --python-exit-code 1 --python art/blender/import_psx_garage.py -- --city 'C:\path\PSX_City_Pack_FREE_SAMPLE.zip' --submarine 'C:\path\PSX_Submarine_Kit_FREE_Sample.zip'
```

Unity imports the generated FBX models and albedo textures from the local `GarageKit` folder.
The Blender script imports a selected set of scenery. Base game controls and objectives are available in both maps.

## Validate this milestone before adding networking

In Unity: **Window → General → Test Runner → PlayMode → Run All**.
The physics suite exercises floor collision, joint stability, doorway dragging,
release/reset, wall occlusion, and whole-body delivery.

From PowerShell (close the editor first):

```powershell
.\scripts\unity.ps1 -Action ImportArt
.\scripts\unity.ps1 -Action Test
.\scripts\unity.ps1 -Action Build
.\scripts\smoke.ps1
.\scripts\smoke.ps1 -Garage
.\scripts\smoke.ps1 -Lab
```

For a custom editor location, add `-Editor 'D:\...\Editor\Unity.exe'`.
Test reports and logs go to `TestResults/`; a successful build goes to
`Builds/Playtest-0.0.10/Hide iT.exe`. These outputs are ignored by Git.
Run the executable with `-hideit-lab` to open the original Carry Lab for physics practice.

Manual acceptance:

- Walk, run, and crouch without crossing solid walls.
- Toggle V near walls and doors; the camera must not leave the room.
- Drag each hand/ankle, raise and lower it, and release it on the steps.
- Pull the body through the opening without teleporting any limb.
- Drag, lift, and crouch with a grip, then release. Check that the hands follow the
  body, the pose settles on release, and the first-person carry arms appear.
- Press R while holding; no old joint should remain.
- Throw a rock away from your route and watch a nearby guard investigate the impact.
- Break sight before hiding and wait for the guard to resume patrol.
- Stash the body in the dumpster, restart, and complete the mission again.
- Pause, switch windows, resume, and confirm input/cursor recovery.

## Next milestone

After the offline checks pass: add host-authoritative networking in **the same test room**,
then verify two real clients holding opposite ends. Then bring the Asylum mission online.
See [the agreed design](docs/GAME_DESIGN.md) and [the milestone gates](docs/MILESTONES.md).

The Carry Lab's props are generated in this project. The local `.tools/` directory holds
editable source and setup files and is ignored by Git.
