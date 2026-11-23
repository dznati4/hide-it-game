# Milestone gates

Only one development milestone is active at a time. A written implementation is
not the same as a compiled, tested, playable result.

| Gate | Scope | Acceptance | Status |
| --- | --- | --- | --- |
| 01 — Carry Lab | Offline movement, V camera, ragdoll, drag, release, reset, delivery | Unity compiles; physics suite and standalone route pass; human feel review | Six tests passed; Windows build and rendered acceptance passed; ready for user playtest |
| 02 — Shared carry | Host plus two clients in the same test room; authoritative body physics | Two real clients carry opposite ends; third player helps; consistent body transforms; disconnect releases grip | Not started |
| 03 — Internet rooms | Real online room codes, three-player cap, lobby/start flow | Join from a different network; fourth participant rejected; host start works; connection errors recover | Not started |
| 04 — Stealth slice | One guard, suspicion, red warning, camera, cover | Innocent walking allowed; suspicious actions detected; LOS blocked by walls; decay after 5–8 seconds | Not started |
| 05 — Rescue loop | Detention, teammate rescue, all-caught failure, extraction | Rescue works on both clients; all-caught fails; extraction requires body and all teammates | Not started |
| 06 — Hotel | First co-op mission, keys/doors, distractions, bystanders | Full online run from lobby to victory; target 8–12 minutes | Not started |
| 07 — Parking | Second co-op mission | Full run, guards/cameras and gate objectives; target 8–12 minutes | Not started |
| 08 — Solo | Dedicated small map and handling aid | One person can finish; capture/restart and extraction work | Not started |
| 09 — Presentation | Original art/audio, UI polish, Steam preparation | Performance and usability checks; licenses recorded | Not started |

## Current setup status

Unity Hub **4.0.1** is installed and the user has completed sign-in. Hub has installed
Unity **6000.6.10f1** to `C:\Program Files\Unity\Hub\Editor\6000.6.10f1`.
The project has been aligned with that existing download, avoiding a second editor installation.
The original official Hub and 6.3 editor installers had valid Windows signatures
from Unity Technologies SF. The unused 6.3 editor was not installed; its download
was removed to recover 3.92 GiB. No paid assets were purchased.

C# syntax, JSON/assembly definitions, PowerShell syntax, project branding, Unity
compilation, all six physics tests, and the standalone acceptance run passed.

Actual results are recorded in [VALIDATION.md](VALIDATION.md). Human feedback on
the feel of dragging remains the next review; later systems are not implemented yet.
