# 07 — Physics, rigging and export pipeline

Research: 22 November 2026. Scope: original **Hide iT / ZanettiZONEai**, zero asset budget, **no blood**, Unity **6000.6.10f1** verified in ProjectVersion.txt. Read reports 01–06, RagdollBody.cs, CarryLabPlayer.cs, CarryLabSmokeTest.cs, the package manifest and build_prototype.py. Recommendations below concern our implementation; reference-game internals remain unknown.

## Preserve the working mechanical contract

Keep CarryLab's 11 rigidbodies, ten CharacterJoints, four hand/ankle grips and SpringJoint grabbing as the baseline. The complete body weighs 61 kg by the current constants. Art replacement must preserve targeting, doorway clearance, turn/release, delivery containment and reset. This pass inspected code; it did not execute Unity or certify the exported FBX.

Separate three responsibilities: authoritative physics objects, a render skeleton/mesh, and gameplay grip metadata. No bevel, hair, pocket or cap should gain collision automatically. Retain existing primitive colliders initially; capsule experiments come later behind comparison tests. Never use the deforming skin as the carried body's collider.

**Highest-risk correction:** the new 22-bone rig is not the existing physics skeleton. Its bent arms, heights and pivots differ. Merely parenting its SkinnedMeshRenderer to the ragdoll root cannot articulate it. Align an authoring copy to the tested collider skeleton, then introduce an explicit mapping with calibrated bind-pose offsets. Map hips/chest/head and upper/lower limbs; derive extra spine, neck, shoulder, hand, foot and toe transforms deliberately. Hands/feet need no new simulated bodies. Drive render bones from physics after simulation; an Animator must not simultaneously overwrite those bones.

## Host authority for 1–3 players

Recommend a listen host owning the entire carried body's simulation, grip creation/release, impulse limits, reset and delivery decision. Clients send grab intentions with stable body/grip identifiers; host validates distance, occupancy and state. Do not transfer authority every time another teammate grabs a limb. That would split one constraint island between machines.

Replicate a coherent body pose plus grip state; interpolate client visuals. One root transform alone cannot reproduce independent limb positions. Non-authoritative copies must not run competing ragdoll forces or award delivery locally. Unity's NetworkRigidbody model makes non-authoritative copies kinematic; it supplies authority behavior, not a complete articulated-body replication solution. [Unity NGO helper components](https://docs.unity.cn/Packages/com.unity.netcode.gameobjects@2.13/manual/components/helper/helpercomponents.html).

NGO is absent from the inspected manifest. Treat networking as a future isolated increment, with one-host/two-client latency testing before co-op is claimed. Keep the same local authoritative interface so solo can reuse it later; budget zero does not itself establish free online hosting.

## Constraints and scale

Current joint axes are uniformly right/forward, with symmetric limits. Preserve the tested settings during the visual swap, then inspect joint gizmos against anatomy. Elbows and knees should predominantly bend in one anatomical plane; shoulder/hip freedom should remain bounded. A bone's displayed direction is not proof of the CharacterJoint's twist direction. Unity exposes separate local anchor, twist and swing axes. [Unity 6.6 CharacterJoint reference](https://docs.unity.com/en-us/engine/6000.6/manual/physics-section/physics-overview/joints-section/class-character-joint).

Current all-pairs IgnoreCollision disables **all** self-collision, despite the comment saying connected limbs. Preserve that baseline deliberately; clothing overlap is a visual issue, while enabling self-collision can introduce instability.

Use unit scales on physics transforms and avoid excessive adjacent mass ratios. Current 16/8 solver iterations are already intentional. Projection is a recovery mechanism, not a substitute for valid spawn placement. If impossible doorway constraints cause separation, compare preprocessing disabled; do not globally change it without regression evidence. Unity recommends locking near-zero axes instead of tiny limits and documents these stability tradeoffs. [Unity 6.6 ragdoll stability](https://docs.unity.com/en-us/engine/6000.6/manual/physics-section/physics-overview/ragdoll/ragdoll-stability).

## Review of the generated rig and FBX

The 22 bones are a sensible inexpensive humanoid inventory, including hands/feet and shoulders. However, every component currently has rigid 100% weighting to one bone. Joined objects remain disconnected islands: bending can expose seams or intersections. Keep this explicitly labeled an articulated style study. For continuous clothing, add joint loops and blended, normalized weights; preserve rigid weighting for hats and shoes where appropriate.

The A-pose is not proof of a valid Humanoid Avatar. Start with Generic for the physics-driven body; use Humanoid when animation retargeting is needed, verify required mapping and enforce/inspect Unity's T-pose. Keep identical names/hierarchy across variants. [Unity avatar import guidance](https://docs.unity3d.com/6000.0/Documentation/Manual/ConfiguringtheAvatar.html).

Current metric units, explicit FBX, selected objects, leaf bones off and animation baking off are reasonable for a static rig study. Pin exporter options and Blender version; animation exports will require a separate bake policy. Leave experimental bake_space_transform off because Blender documents armature/animation problems. [Blender exporter API](https://docs.blender.org/api/dev/bpy.ops.export_scene.html).

Blender faces negative Y here; Unity expects positive Z forward, Y up. The current -Z/Y export convention must pass an actual orientation test, including named left/right limbs and FaceForward. Check a one-metre reference cube and standing height after import; never repair a hundredfold mismatch by scaling the physics parent. Unity distinguishes unit conversion from axis baking. [Unity 6.6 model import settings](https://docs.unity.com/en-us/engine/6000.6/manual/assets-and-media/asset-types/models/importing/fbximporter-model).

## Palette and acceptance

The 128×128 palette with face UVs collapsed to swatch centers is valid for flat color, but is not a conventional surface unwrap or lightmap UV. Explicitly assign one shared Unity material and texture; FBX material translation is not shader parity. Recommend Point filtering, sRGB color and mipmaps off for this tiny palette; compare rendered colors rather than trusting Blender's AgX preview.

Accept only when:

- Import has expected bone count, unit scale, orientation, valid weights, normals and one intended material slot.
- Bend tests reveal no unacceptable elbow/knee holes; grips visibly coincide with hands/ankles.
- Original standalone smoke test still passes without changing collision dimensions.
- Dropped, stretched and offscreen/re-entering ragdolls remain visible. Configure bounds; ragdolls can exceed imported animation bounds. [Unity visibility guidance](https://docs.unity.com/en-us/engine/6000.6/manual/assets-and-media/asset-types/mesh/troubleshooting-skinned-mesh-renderer-visibility).
- Hotel doors, parking gaps and both cameras preserve readability; no blood assets exist.
- Future multiplayer validates simultaneous grabs, release, disconnect and reset under latency before production art expands.

