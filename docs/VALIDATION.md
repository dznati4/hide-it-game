# Carry Lab validation — 22 November 2026

Project: **Hide iT 0.0.1**, **ZanettiZONEai**.
Editor: **Unity 6000.6.10f1 (40e81493c67d)**, Windows.

## Verified

- C# source syntax, JSON and assembly definitions, and PowerShell syntax: passed.
- Unity compilation: passed.
- PlayMode physics tests: **6 passed, 0 failed, 0 skipped**.
- Windows development build: passed; `Builds/Windows/Hide iT.exe` produced.
- Rendered standalone acceptance run on the final build: **passed**.
- Actual menu, delivery, and first-person screenshots were inspected. Ambient fill
  was added because the initial shadows made the test room hard to read.
- Project names and copy use Hide iT and ZanettiZONEai.

The six physics tests verify floor contact/joint stability, dragging through the
doorway, release/reset, selecting an unobstructed grip, rejecting a grip behind
a solid wall, and requiring every limb inside the delivery area.

The final standalone run selects a grip using the normal targeting method,
attaches it, moves the actual CharacterController through the doorway and around
a turn, releases the body in the delivery area, waits for completion, switches
to first person and back, then resets. It uses the real collision and delivery
code. It does not teleport the body through the test route.

## Evidence

Local generated evidence (ignored by Git):

- `TestResults/playmode.xml`
- `TestResults/test.log`
- `TestResults/build.log`
- `TestResults/smoke/result.txt`
- `TestResults/smoke/player.log`
- `TestResults/smoke/01-menu.png`
- `TestResults/smoke/02-delivery.png`
- `TestResults/smoke/03-first-person.png`

Reproduce with `scripts/unity.ps1 -Action Test`, then `-Action Build`, then
`scripts/smoke.ps1`. Close any editor instance for this project first.
The smoke run is visible briefly, ignores desktop input, and closes itself.

## Limits

This is an **offline, single-player mechanics room**. It has no online rooms,
shared carry, guards, cameras, arrest/rescue, mission maps, or trolley yet.
Those remain in the agreed design and milestone list. This room is not the
planned dedicated solo mission. The placeholders are intentional; no art pass
has been attempted.

Automated completion does not replace human feedback about camera comfort,
control feel, or how entertaining the body handling is. The build is ready for
that feedback before the shared-carry milestone.
