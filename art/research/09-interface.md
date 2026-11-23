# 09 — Interface and player feedback

22 November 2026; direction updated 23 November. Hide iT / ZanettiZONEai: original grounded stylized stealth, online co-op for up to three players, hotel/parking setting, English first, Hebrew later, zero asset budget. **No blood, gore, injury overlays, or trail-cleaning interface in the first release.**

## Evidence and implementation boundary

Read research 01–06. Their strongest shared recommendation is readable silhouettes, stable screen regions, and minimal contextual feedback; several explicitly lack verified reference-menu evidence. This proposal does not reconstruct another game's UI. The repository README identifies a tested **offline Carry Lab**; networking, detection, arrest, rescue, and full missions must remain annotated design examples until implemented and tested. Build SVG compositions and an isolated Unity art-gallery scene; do not replace Carry Lab or connect mock buttons to production behavior.

Primary guidance: Microsoft's [XAG 101](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/101) covers readable, configurable text; [XAG 102](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/102) covers contrast and avoiding color-only communication; [XAG 103](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/103) recommends complementary sensory channels. EA's [official skate. accessibility documentation](https://www.ea.com/able/resources/skate) establishes first-screen accessibility access, optional HUD elements, subtitles, and adjustable camera effects as developer-supported examples. These support the principles below, not our proposed stealth rules.

## Visual system and safe layout

Use cream text on opaque charcoal cards, teal focus/interaction accents, amber suspicion, and red only for confirmed alerts or arrest. Restrained outlines and gently clipped card corners can supply personality; keep lettering conventional. Draw original eye, hand, ankle, shield, speech, cuff, and check icons. Pair every state-bearing icon with text; teammate identity also needs a name and slot number. A red silhouette alone cannot explain a chase.

Use a freely licensed sans-serif such as Noto Sans, verifying the included license before redistribution. Target 32–36 px regular body text, 40–48 px section labels, and 64 px titles on a 1920×1080 review canvas. These are proposed design sizes, not accessibility certification: measure rendered glyph height, test contrast, and offer 100/150/200% text scaling without clipping. Provide an opaque background option. Target 4.5:1 text contrast and at least 3:1 for essential graphical boundaries, following [XAG 102](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/102).

Keep critical content 5% inward: x=96–1824, y=54–1026 at 1080p. This is our adjustable desktop margin, not a Unity guarantee. Preserve the central action area. Unity's [Screen.safeArea](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/screen/safearea) reports device-safe pixels; combine that with the chosen inset. For a future UI Toolkit implementation, use [Panel Settings scaling](https://docs.unity.com/en-us/engine/6000.6/manual/uitoolkits/uielements/uie-support-for-runtime-ui/uie-render-runtime-ui/uie-runtime-panel-settings) with a reference resolution and review 16:9, 16:10, ultrawide, and 720p. Screen scaling alone does not provide accessible text scaling.

Keep English labels in string tables and leave flexible wrapping space, consistent with [Unity localization guidance](https://docs.unity.com/en-us/engine/6000.3/manual/best-practice-guides/bpg-uiad-index/localization). Later Hebrew needs a Hebrew-capable font and explicit right-to-left shaping, punctuation, mixed Latin room-code, and mirrored-layout review in the installed Unity version. An English SVG does not validate Hebrew support; do not bake functional labels into textures.

## Menu, selection, and lobby compositions

**Menu:** left column, logo above “Online co-op · Up to 3 players”; buttons “Host game”, “Join game”, “Settings”, “Quit”. Put “Accessibility” beside Settings on the first screen. Show “Solo — Planned” as disabled explanatory text, not a working mode. In the art gallery, add a persistent footer: “UI concept · Online play not connected”. Keep the real offline button “Play Carry Lab” distinct from the concept.

**Selection/lobby:** use one screen, not a separate sequence. Left 60% holds three equal character cards, labeled “Player 1”, “Player 2”, “Open slot”. Each occupied card has name, large silhouette, “Outfit” arrows, and “Ready”/“Not ready” with check/hollow-circle shapes. Outfits are cosmetic; show no classes, stats, perks, or locked purchases. Right 35% contains “Room code”, “Copy code”, “Hotel / Parking”, and “Private room”. All connection details are sample data labeled “Example”, never fabricated successful networking.

Bottom actions: local “Ready”; host “Start mission”; secondary “Leave room”. Annotate proposed start rule: connected occupied slots ready and required player count met. Disable with “Waiting for players” or “Waiting for ready players”, not unexplained gray. Proposed failures: “Room not found”, “Connection lost”, “Retry”, “Back”. Show visible keyboard focus, predictable tab order, and Esc/back behavior. Online pause overlay must say “Online play continues” when that behavior exists; never promise a multiplayer pause.

## Minimal mission HUD and contextual grips

At top left, show one objective, for example “Hide the body”, with a smaller “Service room” destination. Below, three compact rows: “1 Dani · Free”, “2 Alex · Carrying”, “3 Sam · Arrested”. Top center holds the current threat state only. Bottom center shows one interaction card; bottom right holds optional “V Switch view”. No minimap, inventory grid, score, health, or permanent control legend is justified yet.

Highlight only the nearest valid, unoccluded hand/ankle. Draw a small outlined grip ring at that point, with leader line if needed; avoid four simultaneous prompts. Match current Carry Lab inputs: “E Grab left hand”, “E Grab right ankle”; when held, “Holding left hand”, “E Release”, “Q Drop”, “Hold LMB Raise”. Use a mouse-button glyph plus text. Show “Grip occupied” only if multiplayer ownership actually reports it. Never show an interact prompt through a wall. Input hints must eventually follow remapping.

## Threat, civilian, arrest, and rescue states

These are **proposed state contracts**, not implemented AI:

| State | Exact label and visual annotation |
|---|---|
| Unnoticed | Eye outline + “Unnoticed”; quiet neutral card, no persistent NPC labels. |
| Guard suspicion | Partial eye fill + “Guard suspicious”; amber segmented fill tied to real detection progress, no invented percentage. |
| Civilian witness | Speech/eye icon + “Civilian noticed you”; distinct from guard pursuit. “Calling security” only while that action exists. |
| Confirmed chase | Solid exclamation/chevron + “Spotted — Guard chasing”; red marker over visible pursuer and directional edge cue when offscreen. |
| Search after lost sight | Broken eye + “Guard searching”; amber, without a precise through-wall guard location. |
| Arrest | Cuff icon + “Arrested”; teammate row “Sam · Arrested”; no blood/health metaphor. |
| Rescue | At valid interaction: “E Rescue Sam”; “Rescuing Sam” plus progress only if the server confirms it; success “Sam rescued”. |

Use one transition sound alongside each important visual notification; caption meaningful offscreen barks. Reduce motion/flash options must retain icon/text meaning. Review grayscale, muted audio, enlarged text, moving backgrounds, and both camera views. Gallery plates should separately label idle, suspicion, chase, arrested, rescue, and grip examples so reviewers can distinguish proposal from working mechanics.

## 23 November implementation handoff

SVG studies: `art/previews/04-lobby-concept.svg`, `05-hud-concept.svg`. Original vectors, system Arial, 1920×1080. Cosmetic choices: Alex/orange, Noam/olive, Maya/denim, Eli/cream, Roni/plum; guard/guest remain NPCs. All networking/AI is illustrative. “Grab head” is proposed; current grips are hand/ankle. Chase/rescue rail is review-only. No runtime or Carry Lab changes.
