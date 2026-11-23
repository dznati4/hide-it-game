# Agent 03 — ROB RUN reference study

Research date: 22 November 2026. Target: original, zero-budget **Hide iT** art made in Blender for Unity. **No blood in the initial prototype.**

## Identity and primary evidence

The intended game is **ROB RUN**, Steam app **4262460**, developed and published by **Kefir**. This distinguishes it from similarly named running games. Kefir maintains a matching game page. [Steam listing](https://store.steampowered.com/app/4262460/ROB_RUN/), [Kefir game page](https://kefirgames.com/games/rob-run).

The developer describes a large competitive criminal world; its own site presents city/desert settings and police pursuit. That scale and competitive structure differ from our three-person stealth missions. [Official game site](https://robrun.com/en).

## What I actually inspected

I sampled the official 48-second gameplay trailer around its shop sequence and inspected the store's jewelry-shop gameplay screenshot. I did not watch the complete trailer or play the game.

**Observed:** characters have broadly human proportions, recognizable jackets, bags and trousers. The jewelry-shop image distinguishes colorful masked participants from a dark-suited person behind the counter. Its environment combines broad cream/gold surfaces, dark wall panels and small merchandise. The HUD places a map at upper left, team information at lower left and equipment at lower right. This is more grounded than a toy-like silhouette, with conspicuous clothing accents. [Observed store media](https://store.steampowered.com/app/4262460/ROB_RUN/).

**Developer claim:** police and criminals are human players. This reference therefore does not establish how an NPC suspicion system should work. [Steam description](https://store.steampowered.com/app/4262460/ROB_RUN/).

## Proposed translation for Hide iT

The following are **our design proposals**, not claims about ROB RUN's production pipeline.

### Characters: one reusable body, several readable roles

Build one original human base approximately 1.75 metres tall, with slightly enlarged hands and shoes and a head around one sixth of total height. Those are prototype choices to support comedy and visible grip points. Keep shoulders, elbows, hips and knees easy to read during awkward carrying. Give the same base distinct garments rather than creating a separate anatomical mesh for every role.

Use three silhouette tests before adding facial detail: player, security guard and civilian. A guard receives a structured overshirt, peaked cap, broad belt and shoulder radio. A civilian receives softer cardigan/hoodie shapes, irregular hems and no equipment belt. Players receive a compact jacket with a clear personal accent. Police can later gain a separate navy uniform and more formal cap; do not treat every uniformed character as the same role.

Test a charcoal silhouette render from the game camera. Color should reinforce recognition, not carry it alone. Avoid recreating the reference's specific tracksuit graphics, bags, labels or branded uniforms.

### Materials and world kit

Start with six shared materials: cloth, skin, rubber, painted metal, pale wall and dark trim. Use restrained roughness differences and broad color blocks. Surface scratches, fabric weave and skin pores are unnecessary for the first read test. Small face features should survive at gameplay distance before texture work begins.

Create one hotel corridor corner as an art sample, with wall, door surround, floor tile, ceiling panel and service cupboard modules. Use the existing tested doorway dimensions as the constraint; fit the decorative frame around that clearance. Do not narrow collisions to accommodate a prettier frame. Add one security camera shell and one four-wheel trolley shell as separate props. The trolley's appearance does not imply that its physics or gameplay is implemented.

The initial goal is a coherent kit, not an entire city. Keep large floor and wall shapes quiet so limbs, uniforms and an approaching guard remain easy to distinguish. Use soft ambient fill plus a directional/key light; darkness should not become the mechanism that hides unclear models.

### UI and selection screens

Do not import an extraction-shooter inventory layout. Hide iT needs one objective line, three teammate slots, a contextual grab prompt and an explicit suspicion state. Combine an eye icon, a text label and a fill indicator; reserve red for danger while retaining a readable non-color cue.

A first character-selection mockup needs three large character previews, name/accent selection, room code and ready status. Keep the same type scale and spacing in menus and HUD. Avoid ammunition counters, loot grids and progression panels until the actual mission needs them.

### Physics-compatible asset handoff

Keep render geometry separate from physics shapes. Fit simple capsules/boxes to the existing body rather than using deforming character meshes as colliders. Use a common skeleton with predictable bone names and explicit hand/ankle grip markers. Validate a standing pose, a flat dropped pose and a body pulled through the real doorway before embellishing clothes. Do not add simulated clothing, hair strands or finger physics to this prototype.

## Evidence limits and acceptance

Public media does **not** establish exact source models, mesh counts, texture resolutions, shaders, engine settings, rig construction, authoring software or asset-store provenance. Apparent material response is not proof of a particular rendering pipeline. No assets were extracted or downloaded for reuse.

Accept our Blender prototype only when each role is recognizable at gameplay distance, grip locations remain visible, exported scale matches the physics room, decorative meshes preserve passage clearance, and the no-blood constraint is satisfied. These are Hide iT requirements, not measurements of the reference game.
