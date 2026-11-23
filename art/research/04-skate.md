# skate. reference study for Hide iT

Research date: 22 November 2026. Project: ZanettiZONEai; original, cartoony, blood-free, 1–3-player co-op stealth prototype in Unity/Blender, with V switching third/first person. Scope: Full Circle’s current skate., not earlier franchise technology.

## Evidence and limits

The [developer-authored Steam listing](https://store.steampowered.com/app/3354750/) identifies Full Circle and Electronic Arts and describes physics-based skating and off-board exploration. I inspected EA’s Features page and opened its full-size rail and interior promotional images in a browser, plus EA’s Interface settings screenshot. These are visual observations of official stills, not hands-on testing or a frame-by-frame video analysis. Promotional framing may differ from normal play. The Interface image visibly says “Early Pre-Alpha Gameplay”; current behavior is better supported by EA’s accompanying written accessibility documentation. No proprietary meshes, textures, rigs, or animations were obtained.

## Clothing and material readability

**Official statement:** [Board Room 05](https://www.ea.com/games/skate/skate/news/the-board-room-episode-05) describes an art direction between realism and stylization, with customizable wardrobe, body height/shape, and features. That supports using skate. as a readability reference without treating its proportions as Hide iT’s target.

**Observed:** In the [official rail still](https://drop-assets.ea.com/images/IHnDE9ReSX4eB2Va1da2v/f24f9f116db50c5efdda0c505c0f1d6d/SW_Total_ControL_04_NEW.jpg), warm orange/purple clothing separates the skater from broad gray stairs and concrete. Bent knees, spread arms, and a clear torso-to-leg break communicate balance immediately. The tubular rail catches a narrow bright highlight; concrete has broad, restrained variation. Clothing shows folds without making the whole silhouette noisy.

The [official interior still](https://drop-assets.ea.com/images/1bcFlZ4imkodaCZlHjgeiN/fdfda7011997c89e45067d6dc1547618/SW_CYOF_02_New.jpg) uses a black hoodie, pale blue baggy trousers, and shoes with bright sole edges. Limbs remain legible against warm wood and pale masonry, although dark sleeves merge locally with shadow. This shows why silhouette and value separation matter alongside color.

**Proposed application:** Build one original Blender humanoid with chunky shoes, broad hands, and three restrained outfit palettes. Use mostly matte materials and a few broad modeled folds; reserve glossy accents for metal and plastic props. Judge each costume at gameplay distance against both beige hotel walls and gray parking floors. Avoid reproducing skate. clothing prints, logos, or branded shoes. Outfit swaps should reuse our own rig and compatible shapes rather than require a large wardrobe system.

## Body animation versus physics

**Official evidence:** [Board Room 04](https://www.ea.com/games/skate/skate/news/the-board-room-episode-04) describes deciding whether tricks are contextual or explicitly timed, capturing skaters through motion capture, then animation-team work. It also discusses character reactions and physics tuning. Therefore “physics-based gameplay” does not justify calling every body motion an uncontrolled ragdoll. The exact runtime blend is not disclosed here.

**Inference and implementation proposal:** Hide iT needs readable intentional actions before amusing accidents. Author short idle, walk, crouch, lift, drag, and carry poses; keep the carried torso predictably located while allowing limited limb motion. On release, switch to a restrained ragdoll with conservative joint limits. Favor clear contact and weight shifts over continuous floppy locomotion. A shoulder dip, slow first step, and visible grip can sell effort without motion capture. Test a two-player carry through a doorway before polishing clothing: collision, camera, and ownership errors would otherwise obscure the intended comedy. This is our proposed Unity architecture, not a claim about Full Circle’s implementation.

## Urban forms and modularity

**Observed:** The rail image combines stairs, slab walls, planters, repeated window bays, and long rail segments. The interior repeats pointed window bays and roof ribs while a bright central installation supplies a memorable landmark. Repetition is visible; reuse of particular meshes, instancing, topology, or modular dimensions is not verifiable from these images.

**Proposed application:** Give the hotel a reusable door/wall/corner kit, then distinguish corridors with a reception desk, vending alcove, or conspicuous carpet color. Give parking a column/bay/ramp kit, with numbered zones and one bright payment booth. Repetition should teach navigation; landmarks should tell teammates where to meet. Keep uncluttered carry lanes and simple collision around doorframes. Put small dressing near walls so players can read patrol sightlines and body clearance. Two compact loops with a service shortcut offer more useful prototype choices than imitating a city’s scope.

## Camera, HUD, and prototype priorities

EA’s [accessibility documentation](https://www.ea.com/able/resources/skate) lists separate on-board/off-board/climbing follow settings, adjustable FOV, shoulder/hip framing, camera offset, shake controls, and optional nametags/input displays. The [Interface screenshot](https://media.contentapi.ea.com/content/dam/eacom/skate/common/accessibility/interface-01.png) visibly prioritizes large rows and a clear selected state.

For Hide iT, prototype a collision-aware third-person view, a comfortable first-person V toggle, adjustable FOV, and optional shake. Show only teammate identity, current interaction, and a concise carry-state cue. Validate both views while carrying through hotel doors and reversing between parked cars. Do not infer stealth information through walls merely because teammates need coordination. Highest-value lessons are controlled silhouettes, deliberate animation/physics transitions, repeated architectural rhythm, and camera settings that preserve readable interaction.
