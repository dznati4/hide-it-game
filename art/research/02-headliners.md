# 02 — The Headliners: research for Hide iT

Research role: game reference analyst. Date: 22 November 2026.
Project: **Hide iT**, **ZanettiZONEai**. Initial prototype: **no blood**, original assets, zero purchase budget.

## Sources and evidence

- **Primary source:** [KAFI's official Steam store page](https://store.steampowered.com/app/3059070/The_Headliners/?l=english). The developer describes journalists exploring a dangerous New York, taking photographs, and escaping to review their results.
- **Primary visual evidence:** that page's **Reveal Trailer**, sampled around its opening city sequence and approximately 0:16–0:22, plus the first gameplay screenshot in the gallery. I inspected the rendered page, rather than inferring appearance from tags. [Official header artwork](https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3059070/62f137f87bbbe03ff34fe64f79aec4059532e849/header.jpg?t=1761229580) provides a separate stylized face/clothing reference.

**Observed:** the gameplay screenshot combines chunky, simplified people with busy nighttime streets. Characters have prominent heads, hands and footwear; caps, hair shapes, beards, sleeveless shirts, trousers and trainers provide variation. Dark vests with a large PRESS label repeat across different clothing combinations. The city uses recognizable road barriers, shopfronts, traffic signals and dense signs. Saturated advertisements contrast with dark streets; handheld light picks out nearby figures. The visible HUD places health/voice indicators at upper left, expedition value/status at lower left, equipment slots at right, and action labels at the bottom. These are observations of the inspected images, not a full interface audit. The mixture of expressive characters and threatening surroundings supports a comic-danger tone.

## What is not established

The public material inspected does **not** identify the exact character meshes, marketplace packs, polygon counts, texture resolutions, authoring software, rig layout, cloth solver, or physics implementation. I have not extracted assets or inspected game files. Repeated vests suggest a reusable visual vocabulary, but do not prove a modular character system. Repeated building shapes do not establish how the levels were authored. Engine/SDK detections from third-party databases were excluded from the technical recommendation. The trailer samples do not demonstrate every animation or failure state. Browser availability ended during further gallery exploration, so interiors and selection menus are **not** claimed as visually verified here.

## Original direction proposed for Hide iT

Take the lesson of readable people in a believable space. Build a small cast from a shared human base with a deliberately limited wardrobe, rather than attempting photoreal faces or an entire detailed city. Use rounded heads, clear noses/brows, broad hands and stable shoes. Keep elbows, knees, wrists and ankles distinct so ragdoll bends and grip locations remain legible. The carried character should retain an intact, clearly articulated body and closed eyes; there should be no injury meshes, wound textures, or blood effects in this prototype.

Treat clothing as gameplay information. Give the private guard a peaked cap, straight dark jacket, contrasting shoulder panels and a radio. Give a police variant a more structured torso and a different cap/shoulder silhouette with fictional insignia. Give civilians soft clothing, varied trousers/skirts, hats or bags, without matching uniform panels. Player costumes should use a consistent accent and a recognizable upper-body shape. These are **new designs**, not claims about the reference game's NPC wardrobe. Test recognition in grayscale and from behind; a color change alone is insufficient.

For Blender, make one original base mesh, one simple armature, and separate clothing/accessory objects sharing the same proportions and pivots. Author a neutral standing pose and test deep hip, shoulder and knee bends before adding facial detail. Use material slots for skin, hair, cloth, shoes and role accents. An optional single small palette texture can come later. This keeps the first implementation editable with Blender alone and avoids buying sculpting or texturing software. Any triangle or texture budget should be set by Hide iT profiling, not attributed to the reference.

For the world, make a short hotel corridor kit before a whole hotel: wall, floor, ceiling, doorframe, door, service-room sign, bin and cabinet. Follow it with a parking column, barrier and security-desk kit. Reuse the same door opening and character scale as the tested carry room. Decorative handles, trim and fabric folds must not become snagging collision geometry. Keep collision shapes simple and test the longest ragdoll pose through the doorway after every art replacement. Model the visual mesh separately from Unity's physical representation.

## HUD, light and acceptance criteria

Borrow the reference's separation of information into stable screen areas, not its exact layout. Hide iT needs one compact objective, three teammate states, a contextual grip prompt, and an eye-shaped suspicion indicator with a number or text state. Use an outline/source-direction cue and amber-to-red escalation; red warnings remain distinct from blood. Avoid a permanent full-screen red wash that obscures escape routes. A selection screen should show a large character preview, a few outfit choices and an explicit ready state; that is a proposal, not an observed reference menu.

Use warm hotel light and cooler service/parking light with enough ambient fill to see limbs. Save stronger highlights for exits, interaction targets and threat feedback. Do not reproduce a neon city to obtain tension: geometry, patrol timing and clear sightlines can supply it. The first review should ask whether a new viewer can identify player, guard and civilian within one second, locate the correct grip while moving, and see the body behind a doorway. Validate those questions in the existing carry lab before expanding wardrobe or map art.
