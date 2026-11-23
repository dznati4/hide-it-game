# 08 — Art direction: after-hours hotel farce

Research date: 22 November 2026. Scope: original **Hide iT / ZanettiZONEai**, Unity 6.6 / Blender 5.3, zero purchase budget, comic stealth and body carrying for 1–3 players. **No blood, wounds, exposed injuries, or gore.**

## One coherent direction

Choose an aging, cheerfully aspirational hotel after closing: cream plaster, petrol-blue service doors, orange upholstery, dark wood, oversized room numerals, and slightly overconfident geometric signage. Its parking area repeats the hotel's teal canopy and numbering on plain concrete. People are expressive adults with believable weight, simplified clothing, and mildly exaggerated heads/hands. Comedy comes from conspicuous effort and inconvenient architecture: a dignified uniform trying to negotiate an undignified carry.

The six reference reports support complementary lessons: Oar'some and RV contribute readable people and shared physical trouble; Headliners contributes role recognition in tense spaces; ROB RUN contributes grounded clothing blocks; skate. contributes intentional poses and architectural rhythm; GTA V Legacy contributes material hierarchy and navigable urban landmarks. These are design translations from reports 01–06, not permission to copy faces, costumes, logos, layouts, or proprietary assets. Their public images do not establish production budgets or rigs.

## Character silhouette decisions

Use one anatomical base and compatible skeleton, but change garment contours and posture rather than recoloring identical people. Our proportion target is 5.75–6.25 heads tall at approximately 1.75–1.85 m, with broad palms and clearly separated shoes. This is our target, not a reference measurement.

- **Player:** cropped orange work jacket, narrow waist, rolled cuffs, teal knit cap with a small asymmetric fold, and broad cream shoe edges. Carry poses lean forward; hands remain visible outside the jacket. Three players share the cut but receive cap shapes, sleeve accents, and numbered patches so identity survives color loss.
- **Security:** squared shoulder yoke, longer straight navy jacket, peaked cap, equipment belt and one conspicuous shoulder radio. Upright posture and a deliberate pointing arm communicate authority. Use fictional hotel insignia; tiny badges support a silhouette already readable from behind.
- **Civilian:** plum cardigan with an uneven hem, softer shoulder slope, pale trousers and one substantial shoulder bag. Add a second civilian outline later through a long coat or rounder torso, avoiding a rule that gender alone identifies a role.
- **Carried body:** intact skin, closed eyelids, relaxed jaw, blue overshirt and contrasting cuffs/soles. An asymmetric hanging pose distinguishes its state. Blue clothing is supplementary: any character may become limp later. Keep injury imagery absent and avoid turning skin blue.

## Critique of the scripted draft

`art/blender/build_prototype.py` is a useful original blockout: meter scale, role accessories, a common 22-bone rig, explicit FBX exports and one consolidated material are sound starting decisions. This assessment is based on the script; a finished in-engine deformation review is still required.

Uniformly rounded boxes, spherical elbows/knees, identical torso dimensions, mitten ovals and large spherical eyes risk a toy-assembly appearance. Replace the torso with a tapered ribcage, sloping shoulder plane and distinct pelvis; flatten cheek planes, introduce a jaw/chin wedge, recess smaller eyes under a brow ridge, and taper forearms into wrists. Keep large soles but establish heel/toe direction. Use two or three broad garment folds around elbows, hips and armpits instead of many tiny details. Preserve small asymmetries in hair, collars and expression.

More urgently, each primitive receives one bone at full weight. Joining objects does not weld anatomical surfaces or create blended deformation; the torso is largely assigned to Chest. Build connected garment surfaces with two or three support loops around bending joints, then grade weights across shoulder/chest, hip/thigh and elbow/knee transitions. Keep rigid hats/radios single-weighted. Blender documents that vertex-group weights control bone influence and that automatic weights can require manual correction. Validate exported deformation in Unity rather than relying on Blender-only Preserve Volume behavior. [Blender 5.3 Armature](https://docs.blender.org/manual/en/5.3/modeling/modifiers/deform/armature.html), [skinning](https://docs.blender.org/manual/en/5.3/animation/armatures/skinning/parenting.html).

## Palette, surfaces and light

Retain orange `#E98645`, navy `#294759`, plum `#795C87`, cream `#F2E7CE` and teal `#168A86`. Keep floors and background dressing quieter than characters. Check grayscale thumbnails: navy needs pale shoulder/sole breaks against parking shadows; blue body sleeves need light cuffs against slate floors. Reserve amber/red plus icons and text for suspicion and alert. Navigation uses large numbers and shapes alongside color.

Retain the 128 px palette atlas for character block colors. UVs currently sample swatch centers, so this cannot also carry detailed garment textures. Keep point sampling and test Unity compression/mip behavior. The single roughness value makes everything similarly matte; first separate material response through vertex-color-driven smoothness in one shared shader, only if the project needs it. Avoid transparent eyes and hair. Use warm hotel fixtures, cooler parking fill, and restrained contact shadows. Bake static lighting where practical; light probes keep moving bodies readable. Limit overlapping shadow-casting lights. Blender's studio Cycles render is presentation evidence, not Unity lighting validation.

## Our provisional production targets

These are adjustable Hide iT targets, not measured reference specifications: 4,000–7,000 triangles per nearby character, 1,500–3,000 at distance; one skinned renderer/material per character; retain 22 bones until carry tests reveal a concrete need. Small props: 100–800 triangles; trolley/car blocker: 1,000–3,000; wall modules: 20–200 excluding special openings. Spend geometry on silhouette and deformation before badges. Shared materials reduce setup overhead, but an atlas does not merge all animated people into one draw. SRP Batcher reduces render-state updates; inspect real passes with Frame Debugger. [Unity 6.6 optimization](https://docs.unity.cn/6000.6/Documentation/Manual/optimizing-draw-calls-choose-method.html), [Frame Debugger](https://docs.unity3d.com/6000.0/Documentation/Manual/FrameDebugger.html).

## Build order and acceptance

**P0:** fix anatomical forms/weights; test crouch, overhead reach, single-person drag, two-person carry, dropped pose and three-player doorway congestion in both cameras. Recognize roles at actual gameplay distance and stream-view size.

**P1:** build a 0.5 m snapping kit: 2 m wall/floor/ceiling spans, inner/outer corners, door surround, service door, window bay and skirting; preserve the existing tested opening clearance. Add parking column, bay markings, kerb, barrier and simple parked-car blocker. Keep visual trim separate from simple collision; place origins consistently at floor corners or hinge axes. Use reception canopy, vending alcove and payment booth as three landmarks.

**P2:** add luggage trolley, linen bin, lift panel and hiding-container shell, concentrating contrast on grips/openings. Reuse Blender modeling, UV and weight tools, built-in Python generation, Unity material/profiling tools and optional free Krita for signs. No paid pack is required. Approve one corridor/parking corner before expanding maps or the later small solo space.
