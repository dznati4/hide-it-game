# Agent 01 — Oar'some Adventures

Research date: 22 November 2026. Application: original **Hide iT / ZanettiZONEai** assets made in Blender for Unity. The initial prototype contains **no blood**.

## Evidence and limits

Primary sources: [Haymaker Games](https://www.haymaker.games/) and the [official Steam listing](https://store.steampowered.com/app/4555210/). I visually inspected three full-size screenshots linked from the developer's site: [rafting](https://images.squarespace-cdn.com/content/v1/69fd3f426c8c9b52b08d739a/ad35749a-ee06-4d33-8bae-0e335543daf0/CarouselRenders.0026.PNG), [forest/trap](https://images.squarespace-cdn.com/content/v1/69fd3f426c8c9b52b08d739a/10815479-8614-4e7e-b53e-de513c70faac/CarouselRenders.0196.PNG), and [night boathouse](https://images.squarespace-cdn.com/content/v1/69fd3f426c8c9b52b08d739a/18483f3b-09f2-461d-ac37-1fe79313778e/CarouselRenders.0071.PNG).

The developer links an [official trailer](https://www.youtube.com/watch?v=jNCrFF8q4G0), but trailer playback could not be verified in this pass. Screenshots establish appearance, not motion, control feel, or frame rate. No source inspected establishes exact model files, asset-store provenance, triangle counts, texture sizes, skeletons, rendering engine, or networking architecture. Do not present those as discoveries.

## Observed visual direction

**Characters:** The rafting and boathouse images show rounded, stocky people with prominent noses and simple, readable facial features. Clothing and accessories carry much of the identity: caps, a headband, a plaid shirt, bright life jackets, and different shirt colors. Characters remain recognizable while seated or partly submerged. This is a useful model for preserving player identity while their limbs overlap a carried object. The evidence supports stylized proportions; it does not establish an exact head-to-height ratio. [Rafting image](https://images.squarespace-cdn.com/content/v1/69fd3f426c8c9b52b08d739a/ad35749a-ee06-4d33-8bae-0e335543daf0/CarouselRenders.0026.PNG), [boathouse image](https://images.squarespace-cdn.com/content/v1/69fd3f426c8c9b52b08d739a/18483f3b-09f2-461d-ac37-1fe79313778e/CarouselRenders.0071.PNG).

**Materials and environments:** Broad rock planes and simplified trees sit beside dense grass, reflective water, patterned cloth, and detailed props. It is not merely uniformly flat low-poly art. The trap has a strong toothed outline and dark metal appearance against a clear dirt path. Large vegetation and rock masses organize the scene; fine foliage decorates their edges. Repetition of trees and rock forms suggests reusable families, but screenshots cannot prove the underlying modular construction. [Forest image](https://images.squarespace-cdn.com/content/v1/69fd3f426c8c9b52b08d739a/10815479-8614-4e7e-b53e-de513c70faac/CarouselRenders.0196.PNG).

**Lighting:** The boathouse image contrasts a warm, illuminated interior with a blue exterior. Architecture remains understandable through beams, roof slopes, and openings. For Hide iT, warm hotel public areas and cooler service routes can communicate different spaces without extra interface labels. This is a design inference, not a claim about Oar'some's navigation system. [Boathouse image](https://images.squarespace-cdn.com/content/v1/69fd3f426c8c9b52b08d739a/18483f3b-09f2-461d-ac37-1fe79313778e/CarouselRenders.0071.PNG).

## Co-op and physical comedy

The official listing describes coordinated rowing, cooperative puzzles, branching routes, and damage from bumps and crashes. A shared vehicle makes conflicting player intentions visible. Hide iT can apply that principle to one body: its orientation and snagging should reveal why teamwork succeeds or fails. This does not justify copying a boat controller, or claiming the reference uses ragdoll simulation. Unpredictability should arise from understandable collisions and opposing forces rather than random impulses. [Official Steam description](https://store.steampowered.com/app/4555210/).

## HUD and selection screens

The inspected promotional screenshots omit a gameplay HUD. They provide no reliable evidence for health bars, interaction prompts, lobby layout, or inventory controls. My recommendation for Hide iT is therefore independent: three compact teammate indicators, one contextual interaction prompt, and a suspicion indicator combining icon, text, and color. Preserve visibility around hands and feet. Use a simple character-selection lineup with role-neutral outfit colors; do not imply that clothing grants abilities before that mechanic exists.

## Original Blender prototype proposal

Build one reusable humanoid family first. The following are **our design targets**, not measurements of the reference: approximately 1.8 m standing height, slightly enlarged head and hands, broad readable footwear, and a neutral rig pose. Make three costume variants from shared topology: orange/teal player clothing, a navy security uniform with cap and distinct shoulder shape, and a civilian jacket or skirt/trousers silhouette. Uniform identity must survive grayscale and partial occlusion; color alone is insufficient.

Use restrained material slots for skin, hair, clothing, footwear, and an accent. Start with solid colors and rough surfaces. Add only one simple fabric pattern after the silhouette works. Make the body visually distinct through costume and limp posing, with no blood or exposed injuries. Keep render meshes separate from simple physics colliders; visual bevels must not silently narrow the tested doorway.

For the world, model a tiny hotel kit: straight wall, doorway, corner, floor tile, service door, locker, and trolley. Give each an explicit meter scale and useful pivot. Take the reference's readable large forms and warm/cool separation; postpone dense foliage, complex water, and expensive surface detail because they do not test Hide iT's central interaction.

## Acceptance and recommendation

Render the lineup from front, side, back, and the actual third-person camera. Check silhouettes as small grayscale thumbnails. Import one character and one doorway into the existing Carry Lab, then repeat targeting, dragging, snagging, and delivery checks. Reject any prettier mesh that hides the grip or changes collision behavior unexpectedly. The strongest transferable lesson is cheerful, readable characters against organized scene masses. Exact assets and production techniques remain unverified; create original equivalents.
