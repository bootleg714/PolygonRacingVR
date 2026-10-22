# Polygon Racing VR — current development handoff

Updated: October 21, 2026 (America/Chicago).

This replaces the previous contents of this handoff. It supersedes the old v01/v02 instructions: slipstreaming, the coastal circuit, marina, minimap, banking and the seaside venue are now implemented and user-tested. Do not restart from the old “implement slipstreaming next” plan.

## Resume here

Thomas has a working standalone Quest 2 arcade racer. The current accepted baseline combines **Coastal Style v11 Seaside Paddock**, **v10 Gantry Lettering Fix**, **v09 Start Gantry**, earlier coastal dependencies, and **Rival Pace v02 Arcade Corners**.

Latest feedback: Thomas likes the faster opponents and noticed their avoidance behavior. They previously lost ground in tight bends because AI alone braked for curvature while the player could stay flat out. The latest rival patch removes that curvature speed cap, retaining traffic slowing, lane changes, drafting and existing rubberbanding. Do not add corner braking back yet.

Immediate next step: keep this baseline and race several rounds to judge whether drafting supports repeated exchanges of position. Only adjust catch-up or baseline pace if those tests show an actual remaining problem. There is no newly approved implementation beyond this handoff.

## Graphics revamp — Look v01: sky, light and water (in testing)

Thomas asked for a graphics revamp done entirely in code (no image generation). Agreed order: sky/lighting/water/shader first, then car and cockpit, then scenery models. Look v01 is committed but **not yet headset-tested**.

What it does (no geometry, route, cockpit, tracking or gameplay changes):

- `PolygonFlat.shader` (Resources): flat-lit polygon shader. Facet normal from screen-space derivatives of camera-relative position, oriented toward the viewer (no reliance on mesh normals or platform derivative sign). Half-wrapped sun + sky/ground hemisphere ambient (material properties, not Unity's ambient probe), mild rim tinted by fog colour, contact darkening on the lower 1.6 m of vertical faces, emission kept (start lights), radial per-pixel linear fog. SRP-batcher layout, stereo/instancing macros, DepthOnly pass.
- `PolygonWater.shader`: flat water geometry with per-pixel animated triangular-lattice facets (7 m cells, 0.85 m virtual waves), facet colour variation, Fresnel sky reflection (horizon = fog colour), sun glints. Facets fade out 60–320 m and the lattice math is skipped past the fade.
- `PolygonSky.shader`: skybox gradient (zenith → horizon = fog colour), sun disc/halo from the main light.
- `PolygonClouds.shader` + generated mesh: 16 faceted cloud puffs 7–17° above the horizon, re-centred on each eye in the vertex shader so they sit at infinity (ring radius 1100 m coastal / 650 m forest, inside the far clip planes 1400/850). Mesh bounds are huge so it is never culled.
- `PolygonLook.cs` (Runtime): per-layout presets and `LitShader()`; `PolygonWorld.Paint`, `PolygonRace` panel and `PolygonRival` suit/helmet materials now use it.
- `PolygonLookSetup.cs` (Editor): menu **Polygon Racing → 9 - Apply Sky, Light and Water Look**. Backs up both scenes to `PolygonRacingBackups`, converts Simple Lit materials in place (colour, emission and cull kept), converts water materials only on water objects (merged landmark meshes that share the lagoon colour get a flat “twin” material in `Generated/Look`), sets sun, trilight ambient, fog, skybox and camera clear flags, rebuilds clouds. Menus 6 and 7 call the same apply step, so a coastal scenery refresh keeps the look.

Presets: coastal sun Euler(38,−35) colour (1,.94,.82) ×1.3, fog 380–1300 m, horizon (.66,.86,.93), zenith (.16,.46,.82). Forest sun Euler(42,−35) ×1.25, fog 220–800 m, horizon (.70,.84,.88). Shared ambient sky (.60,.74,.86), ground (.50,.42,.33) × 0.75. Tune live on materials / Lighting window; re-running menu 9 resets to the presets in `PolygonLook.cs`.

Install: pull, let Unity import (check the Console for shader errors — magenta means a shader failed), run menu 9 once, Play in editor, then Build And Run. Watch: shader errors, water shimmer or sparkle in the headset, frame rate on the coastal straight, whether the sun disc/clouds look right in both eyes, cockpit readability.

Headset result for Look v01: accepted, FPS good with no dips, water facets visible but too subtle. Water strengthened in the next commit (cell 8 m, waves 1.5 m, speed 1.2, tint 0.22, fade 90–420 m, Fresnel 0.35, glint 2.0 / power 60); menu 9 writes these values onto the existing water materials.

### Car and cockpit — Car v01 (in testing)

Menu **Polygon Racing → 10 - Rebuild Player Car (Faceted)** (`Editor/PolygonCarSetup.cs`) backs up both scenes and replaces the player car's old box parts with one multi-material faceted body mesh: lofted nose with ivory centre stripe, open cockpit tub, shaped sidepods with dark intakes, engine cover/airbox, floor, diffuser strakes, two-element front wing, rear wing with swan-neck pylons, wishbones/pushrods and mirrors. It also rebuilds the steering wheel (flat-bottom rim, grips, centre plate with coloured buttons, yellow top marker) under the existing `Steering wheel` pivot, and gives each existing tyre an 18-sided chamfered mesh plus a rim child with five ivory spokes and a metal nut.

Kept exactly: car root and `PolygonDriver` values, `Seated tracking space`/camera at `(0, 1.03, −0.53)`, the tuned `Dashboard` box (0, .8, .43; scale .4×.15×.1) and `Live instruments` text, steering/wheel pivots and the `wheels`/`frontSteering`/`steeringWheel` references. Only known old part names are deleted. Steering-wheel top stays at y ≈ .83 so it does not cover the instrument text from the seat.

Meshes and seven double-sided flat-lit materials live in `Assets/PolygonRacing/Generated/Car`. The livery material is `PR_Vermilion car body` (name prefix required: rivals recolour `PR_Vermilion*`), so rivals get the new body in their team colours automatically. Re-running menu 10 reuses the same assets and keeps any colours tuned in the Inspector. Body is about 2,000 triangles in one renderer.

Headset result for Car v01: “massive difference”, FPS good, water now clearly visible. Feedback: suspension top bars ended in mid-air; steering-wheel rim segments were asymmetric/hollow (grip test depended on segment order, open square tubes); wanted tilt, a smoother rim and more button detail.

Car v02 (menu 10 again): metal uprights on each wheel pivot (front ones steer, with a steering arm), flat aero wishbones that start just inside the real body surface (`BodyHalfWidth` intersects the nose/engine/sidepod sections) and end on the uprights, plus pushrods. Steering wheel is one continuous swept 6-sided tube around a flat-bottomed oval (40 samples, grip thickness from |cos t| so it is symmetric), carbon centre plate, teal display, 9-segment LED strip (static until the tachometer pass), octagonal buttons, two rotary knobs, shift paddles, yellow top marker. Tilt 15° comes from a new parent `Steering column - tilted`; `PolygonDriver` still rotates the `Steering wheel` transform about its own axis. Two new materials: `PR_Car LED green`, `PR_Car display`.

Colours were chosen with a Python software preview that mirrors the shader maths (not Unity output). It lives outside the repo.

## Project, workflow and validation

- Platform: Meta Quest 2 standalone, Windows Unity project.
- Unity: 6000.6.4f1; URP + OpenXR. Original starter listed URP 17.6.0 and Input System 1.20.0; check the current manifest before changing packages.
- Working title: Polygon Racing VR. Coastal venue branding: POLYGON COAST.
- Previously used Windows folder: `C:\Users\User1\Downloads\Polygon Racing VR`.
- Workflow (from October 21, 2026): the repo is `bootleg714/PolygonRacingVR` on GitHub. The assistant commits small changes to `main`; Thomas pulls them with GitHub Desktop, lets Unity compile, runs any editor menu the change needs, then uses Build And Run over USB and reports headset results. ZIP patches are retired. Revert a bad change in GitHub Desktop (History → Revert changes in commit).
- Codex in VS previously resolved workspace/build issues and implemented working slipstreaming and the original larger coastal layout. Preserve those additions.
- No Unity editor, C# compiler or Quest is available in this assistant workspace. Numerical/source checks and independent software geometry previews were used; these are not Unity screenshots or hardware performance measurements.
- Thomas has tested and accepted the delivered features below. No exact measured FPS for this racing project was established here. Maintain the Quest performance target and keep checking on device.
- Work incrementally. Keep working Android/OpenXR/build-profile settings, tuned cockpit and dashboard dimensions intact.

### Essential install distinction

Gameplay-only script changes generally need compile/build only. Generated-scene changes also require regeneration:

1. Stop Play Mode; save.
2. Replace scripts in `Assets/PolygonRacing/Runtime`, preserving existing `.meta` files.
3. Place required shaders in `Assets/PolygonRacing/Resources` and let all files import/compile.
4. For coastal scenery changes run **Polygon Racing → 7 - Refresh Coastal Expressway Scenery**.
5. Save and Build And Run.

The latest Rival Pace v02 patch needs **no scenery refresh**.

Menu 7 preserves the player/cockpit, rebuilds scenery, persists generated meshes/materials, and saves the CoastalExpressway scene. It backs up the scene under `PolygonRacingBackups` beside Assets. Its generated-art directory is named `Assets/PolygonRacing/Generated/CoastalExpresswayArtV02`; that directory name does not indicate the current patch revision.

Do not rerun the original circuit/car creation command to update scenery. Avoid duplicate classes, `Assets/Assets` nesting, or applying an old whole-project ZIP over the current project.

## Design direction

- Fast, colorful, flat-shaded arcade racing with an editable polygon cockpit/car.
- Large circuits, long drafting straights and broad sweeping bends; preserve the sensation of speed.
- Strong original coastal identity: turquoise sea, warm sand and cliffs, sculpted palms, ivory/teal/coral architecture, maritime roofs and flags.
- Add a few considered elements at a time; judge scale, readability and comfort from the seated headset view.
- Tight corners currently do not require the player to slow. AI must use the same arcade assumption.
- Future higher speeds, understeer and oversteer should be introduced deliberately; corner braking then becomes relevant to both player and AI.
- Preserve natural HMD tracking and recentering. No artificial camera shake. The accepted 4-degree bank rolls the car/cockpit mildly while leaving the tracking code intact.
- Reference another game's ideas without copying its designs. Thomas explicitly reserved the dense urban reference screenshots for a future downtown city race, not this coastal venue.

## Current playable features

- Seated cockpit, natural head tracking, recenter, arcade throttle/brake/steering.
- Forest Circuit and Coastal Expressway, selected through the race menu.
- Three opponents: blue, yellow and green; four-car field.
- Three-lap races, countdown/start lights, race position, lap timing, close-finish interpolation and results.
- Solo Time Trial with locally saved best laps per layout.
- Shared player/AI slipstreaming, dashboard draft feedback and short passing carryover; disabled in Time Trial.
- Existing smooth race-distance rubberbanding and configurable shared rival baseline pace.
- Rival following-distance slowing, lane changes and passing checks. Cars do not have physical car-to-car collision response.
- Player engine, spatial opponent engines, tire sound, gravel rumble and gentle controller pulses.
- Coastal drive-through marina lane with an 80 km/h limit and appropriate race-progress mapping.
- Small translucent car-mounted minimap showing the actual circuit, car headings, opponents, start line and marina branch.
- One gently banked coastal sweeper.
- Coastal landmarks, festival stands, functional red starting signals, corrected two-sided gantry lettering, tower and static team paddock.

### Controls

| Action | Quest | Desktop |
| --- | --- | --- |
| Steer | Left stick horizontal | A/D or arrows |
| Accelerate | Right trigger | W/up arrow |
| Brake | Left trigger | S/down arrow/Space |
| Menu navigation | Left stick up/down | Up/down arrows |
| Confirm menu | A | Enter |
| Restart current mode | A | R |
| Return to menu | Y | M |
| Pause/resume | X | P |
| Recenter | B | C |

## What happened in this conversation

Earlier work established the working four-car racer, fixed race positions and Time Trial, sound/haptics and a longer straight. Thomas then used Codex in VS to resolve issues and get slipstreaming working. The uploaded project already contained that implementation and multiple-circuit support. The major work here was coastal art, scene integration, track features and AI pace.

### Coastal updates and accepted results

| Patch | Delivered work | User feedback/status |
| --- | --- | --- |
| Coastal Style v01 | Replaced sparse coast with colorful sculpted palms, beaches/shallows/surf, warm rock formations, offshore islands, lighthouse, boats, festival scenery and rebuilt bridge | Accepted; substantial visual improvement |
| Rival Pace v01 | Shared configurable faster baseline and smooth gap-based catch-up/leader easing | Fast initially and drafting useful, but rivals still fell behind after being passed; tuning deferred during scenery work |
| Coastal Style v02 | Corner chevrons, curb guidance, Coral Sweep name/pennants/lookout | Looked good; overlapping sign/road colors flickered |
| Coastal Style v03 Marina | Drive-through marina pit, nearby shore setting, rebuilt adjacent road/paint/curb surfaces to remove overlays | Accepted; remaining bridge arch/barrier flicker reported |
| Coastal Style v04 Map | Car-mounted transparent minimap and rebuilt bridge joints/deck/parapets | User explicitly confirmed flicker gone and map works well |
| Coastal Style v05 Banking | First 4-degree banked sweeper, matching road/verge, player/AI poses and nearby scenery | Headset-tested and accepted |
| Coastal Style v06 Sunbay Cove | Carved curved estuary beach, sheltered water/surf, three moored sailboats, six colorful waterfront houses, promenade/landing | Everything worked and looked great |
| Coastal Style v07 Amber Point | 160 m inland cliff gallery, three short overhead stone fingers, ocean side open | Accepted |
| Coastal Style v08 Seaside Grandstands | Six bays, six rows per bay, peaked sail canopies, 589 spectators with heads/shirts/caps/some raised arms | Accepted |
| Coastal Style v09 Start Gantry | POLYGON COAST header, checkers, ivory supports, five countdown-driven reds; floating countdown hidden when physical signal exists | Looked good; user caught rear title showing through |
| Coastal Style v10 Gantry Lettering Fix | Dedicated depth-tested font shader plus view-side title selection and font-atlas rebinding | User explicitly confirmed text fixed |
| Coastal Style v11 Seaside Paddock | Replaced old pavilions with control tower, three open team shelters, static coupes, tire stacks, equipment and maritime team flags | Accepted; track feels more fleshed out |
| Rival Pace v02 Arcade Corners | Removed AI-only curvature speed cap; kept traffic slowing, drafting, catch-up and bank alignment | User likes it: opponents faster and appear to avoid collisions |

### Exact current patch filenames

Scenery sequence:

- `PolygonRacingVR_CoastalStyle_v01.zip`
- `PolygonRacingVR_CoastalStyle_v02.zip`
- `PolygonRacingVR_CoastalStyle_v03_Marina.zip`
- `PolygonRacingVR_CoastalStyle_v04_Map.zip`
- `PolygonRacingVR_CoastalStyle_v05_Banking.zip`
- `PolygonRacingVR_CoastalStyle_v06_SunbayCove.zip`
- `PolygonRacingVR_CoastalStyle_v07_AmberPoint.zip`
- `PolygonRacingVR_CoastalStyle_v08_SeasideGrandstands.zip`
- `PolygonRacingVR_CoastalStyle_v09_StartGantry.zip`
- `PolygonRacingVR_CoastalStyle_v10_GantryLetteringFix.zip`
- `PolygonRacingVR_CoastalStyle_v11_SeasidePaddock.zip`

Opponent pace sequence:

- `PolygonRacingVR_RivalPace_v01.zip`
- `PolygonRacingVR_RivalPace_v02_ArcadeCorners.zip`

These are incremental patches, not complete projects. Coastal Style v11 alone does not contain the earlier new scripts/shaders. Rival Pace numbering is separate from Coastal Style numbering. Old gameplay v11/v12/v13 ZIPs are a third, earlier sequence and must not replace the current scripts just because their numbers look higher.

## Latest file ownership — important for avoiding regressions

Runtime files live in `Assets/PolygonRacing/Runtime` unless noted.

| File | Latest source in the accepted patch stack |
| --- | --- |
| `PolygonCoastalArt.cs` | Coastal Style v11 Seaside Paddock; includes all prior art, banking call, gantry and lettering wiring |
| `PolygonRival.cs` | Rival Pace v02 Arcade Corners; includes v05 banked pose and v01 catch-up/draft logic |
| `PolygonRace.cs` | Coastal Style v09 Start Gantry; retains marina race projection/slipstream logic and exposes light count |
| `PolygonDriver.cs` | Coastal Style v05 Banking; retains v03 pit handling and existing draft/VR tracking |
| `PolygonMarinaPit.cs` | Coastal Style v05 Banking; retains v03 marina and adds banked road vertex heights |
| `PolygonBanking.cs` | New in Coastal Style v05 |
| `PolygonMiniMap.cs` | New in Coastal Style v04 |
| `PolygonMiniMap.shader` | v04; `Assets/PolygonRacing/Resources` |
| `PolygonStartLights.cs` | New in Coastal Style v09 |
| `PolygonGantryLettering.cs` | New in Coastal Style v10 |
| `PolygonGantryLettering.shader` | v10; `Assets/PolygonRacing/Resources` |
| `PolygonRacingFeedback.cs` | Coastal Style v03 marina integration |
| `PolygonRivalPaceSettings.cs` and its editor helper | Rival Pace v01; retain existing user settings asset |
| `PolygonWorld.cs` and `PolygonCoastalExpresswaySetup.cs` | Coastal Style v01 integration/editor workflow |
| `PolygonCircuit.cs`, `PolygonCircuitSelector.cs`, `PolygonSlipstream.cs`, engine code | Working uploaded/VS project; unchanged by recent art/pace patches |

The last full uploaded source archive was `Polygon Racing VR(2).zip`. It predates this coastal patch stack. Obtain a fresh ZIP of Thomas's installed project before future coding if the current merged source is unavailable. Never treat that upload as the latest whole project.

## Geometry, race and VR architecture

### Shared route

- Closed cubic B-spline with 1024 samples and an arc-length lookup table.
- Forest: approximately 1,440 m, road half-width 9 m; earlier v12 added a 280 m exactly straight section.
- Coastal Expressway: approximately 3,954 m, road half-width 11 m, 28 control knots; long straights and sweeping sections.
- `PolygonCircuit` owns route samples, tangent, curvature, projection and layout identity. `PolygonWorld.Point/Side/Tangent` wrap that route.
- Main route and race distance intentionally remain in XZ; bank elevation is a separate shared surface function.
- Saved road meshes do not update merely because a route/builder script changed. The earlier invisible/out-of-bounds road mismatch came from failing to regenerate saved scenery.

### Race logic to preserve

- Player progress is unwrapped route projection, with rejection for implausible large jumps/off-course movement.
- Rivals report actual accumulated `RaceDistance`; do not replace it with raw wrapped projection of visual position.
- Marina progress maps its branch back to main-route race distance.
- Preserve seam crossing, reverse behavior, finish-time interpolation, race locks and locally saved Time Trial layout keys.
- `+1L` means an actual lap deficit; physical overtaking does not erase one. `FIN` identifies finished rivals.
- Menu hides/locks opponents in Time Trial. Keep draft state reset on mode changes, restart, menu and finish, and frozen/disabled appropriately during pause/countdown.
- Rival visuals copy player meshes, excluding tracking, cameras, TextMesh, dashboard and cockpit race panel. That exclusion fixed the old black square in front of rivals.

### Tracking and update order

- Preserve manual HMD pose tracking; the tracking-space transform compensates to the seated point without overwriting natural headset motion.
- Seat constant is `(0, 1.03, -0.53)` in the current driver; preserve manually tuned cockpit/dashboard scale.
- Circuit selector initializes before gameplay; race runs at execution order 100, start lights 150, racing feedback 200, minimap 250.
- Gantry lettering is ordinary LateUpdate camera-side selection; it does not alter the headset or car.

### Slipstreaming — already complete

Current source constants, before any future user tuning:

- Up to 8% top-speed and 12% acceleration bonus.
- Valid longitudinal route gap 4.5–28 m; full range strength by 9 m.
- Lateral tolerance 5.2 m, full lateral strength within 3.5 m.
- Buildup 1.25 s, carryover 1.1 s, fade 0.55 s.
- Requires suitable car/route heading alignment and real forward separation; rejects adjacent-track false matches and off-road cars.
- Shared player/AI implementation. Dashboard updates belong to `PolygonRace`.
- Time Trial has no draft benefit.

## Current AI baseline and the key handling lesson

### Rival Pace settings

Menu **Polygon Racing → 8 - Opponent Pace Settings** creates/selects `Assets/Resources/PolygonRivalPace.asset`. Do not recreate/reset it during unrelated patches. Runtime can use defaults if missing.

Default v01 settings (check the installed asset for any later manual values):

| Setting | Default |
| --- | --- |
| Shared nominal top speed | 1.01 × configured player top speed |
| Acceleration | 0.98 × configured player acceleration |
| Neutral gap | 40 m |
| Full gap adjustment | 220 m |
| Maximum catch-up | +8% |
| Maximum leader easing | −4% |
| Response blend | 4 seconds |

At the driver's default 42 m/s, nominal rivals are about 153 km/h before draft/catch-up. Rival colors retain distinct grid/lane behavior, not intentionally different baseline speeds.

Catch-up uses unwrapped player-minus-rival race distance and blends gradually. It does not teleport cars. Existing settings/tooltips may still describe “straight” speed and `cornerGrip`; following Rival Pace v02, nominal speed modifiers apply through bends too, and the retained `cornerGrip` field no longer caps speed.

### Why rivals previously fell away

Thomas observed that he passed opponents mainly at tight turns. AI used `sqrt(grip / curvature)` while the player had no equivalent traction limit. Numerical inspection at the default player speed found the old tightest coastal corner cap around 85 km/h versus roughly 153 km/h nominal rival pace. Stronger rubberbanding would have masked this asymmetric handling rule rather than fixing it.

Rival Pace v02 removes curvature lookahead/braking and the unused Grip accessor. It retains:

- Following-distance slowing when another car occupies the lane.
- Lane-change/passing checks and the same lane-change rate.
- Existing acceleration/braking response to traffic and speed modifiers.
- Catch-up, leader easing and draft behavior.
- Route-following, race distance, wheel steering animation and banked surface pose.

Thomas reported faster rivals and apparent avoidance after testing. The avoidance itself was already present; the patch did not introduce physical collision response or a new avoidance algorithm.

## Coastal features and implementation lessons

### Flicker: repair geometry instead of adding another overlay

Overlapping or nearly coplanar faces caused the reported sign/road/arch flicker.

- Road asphalt, lines and curb bands now occupy adjacent polygons, using matching longitudinal boundaries (4 m sections plus lane-dash/feature cuts).
- Chevron foreground and background are adjoining inlaid shapes.
- Bridge deck is an open-top shell; road and footpaths provide its top rather than competing with a slab immediately below.
- Bridge post tops meet beam undersides; parapets are split around markers/supports.
- Generic roadside rails/posts near the dedicated bridge are removed.
- Cove surf replaces water polygons in its band rather than being painted directly above them.
- New canopy panels/checkers/flags avoid stacked colored faces. Inspect both near and far in VR.

User explicitly confirmed the bridge flicker was gone in v04.

### Marina

- Entry at knot 11.2 (~1,739 m), exit knot 14.9 (~2,357 m).
- Offset reaches 42 m with 100 m eased entry/exit; lane half-width 5.5 m.
- Speed limit 80 km/h. Driver/feedback/race projection understand the branch.
- Scenic drive-through lane only; repair/refuel/service gameplay is future work.

### Minimap

- Small translucent teal panel to the right of the dashboard, attached to the car, not the head.
- Static line geometry plus moving markers, no second camera or render texture.
- Ivory course/player marker; blue/yellow/green rival dots; mint start mark; coral pit branch.
- Forest uses its own route. Time Trial omits rivals. Map hides while race-locked.
- Custom URP shader in Resources has VR stereo macros. Keep it included in builds.

### Banking

- Coral Sweep centered at knot 9.4 (~1,467 m), window ~1,317–1,617 m.
- Maximum 4 degrees: 100 m easing in, 100 m hold, 100 m easing out. Ends over 100 m before marina entry.
- Raises the surface rather than excavating ground; center rises up to ~1.4 m.
- Road, markings, verge and nearby rails/signs/palms follow the surface; player and AI use its height and normal. Forest remains flat.
- Numerical normals use nearby surface samples. Verge/road cuts must match to avoid cracks.
- Steering integrates horizontal heading/movement first, then applies vertical surface alignment. Solve the forward vector's Y component to lie on the bank while preserving its XZ heading.
- Do not repeatedly project the entire car-forward vector onto the tilted plane: that can subtly change XZ heading and accumulate yaw drift.
- This is geometric banking, not suspension/gravity/traction simulation. Tracking code was preserved exactly and Thomas accepted the headset feel.

### Landmark placement and static geometry

- Sunbay Cove reshapes the original estuary south bank, not an overlay on uncut grass. Six houses, three moored sailboats, promenade and landing are on the bridge's south/east side. Look right crossing from the start straight.
- Amber Point is a 160 m inland rock gallery around 1.50–1.66 km. Three overhead fingers have a minimum 14 m datum height, over 11 m above maximum bank height. Ocean side stays open; it ends before marina entry.
- Seaside stands have six bays and 589 static spectators. Head/body silhouettes and repeated sail roofs create density without individual animated agents.
- Paddock replaces five old pavilions. Tower at ~116 m/lateral −53 m; team bays at 96/132/168 m/lateral −77 m. Closest tower/bay footprint is about 45 m from road center. Parked cars are static props, not extra opponents.
- Remove palms occupying new structures before baking. Use exact route projection/footprint checks, not just center-point clearance.
- `MergeLandmarkMeshes` bakes each stand, Sunbay village and paddock into meshes grouped by palette material. Nested transforms are baked via the root's inverse/world matrices.
- Existing materials are reused; meshes are persisted by the editor workflow. Most new scenery has no colliders, updates, extra lights or cameras. Do not call that proof of a measured FPS gain.

### Start gantry and lettering fix

- Gantry placed 32 m ahead of the original grid/start position so the driver can see the signals. Lap timing line is unchanged.
- Broad ivory supports, coral collars, teal crown, checkered panels and POLYGON COAST titles in both directions.
- The race exposes `StartLightCount` from its actual remaining countdown: first red at 0 s, then 0.5/1/1.5/2 s; all out at 3 s when race unlocks. No green phase or independent timer.
- Menus/driving/results show no red lights. Pause holds the countdown/light state; restart repeats it. Forest keeps its original countdown panel.
- The floating countdown/GO panel hides when a physical gantry exists so it cannot obscure the signals. Menus/results still appear.
- Red and off materials are both initially assigned to scene renderers so the existing editor persistence pass saves both. A material held only by a MonoBehaviour would not be found by that renderer-based save loop.
- v10 fixes simultaneous forward/mirrored lettering: custom font shader uses scene depth (`ZTest LEqual`), and `PolygonGantryLettering` enables only the title facing the main camera. Both VR eyes share that side choice.
- Bind the custom material to the dynamic font atlas on enable and `Font.textureRebuilt`; unsubscribe on disable. Keep URP stereo macros and text vertex color/atlas alpha support.
- User explicitly verified the text fix. Preserve it when replacing `PolygonCoastalArt.cs`.

## Reference images reserved for downtown

Thomas supplied these screenshots and explicitly chose to save their design ideas for a future downtown city race:

- `image(20261021-220913).png`
- `image(20261021-220930).png`
- `image(20261021-220946).png`

They show dense urban grandstands, repeated canopy structures, bold team flags, tall buildings and a busy gantry/start straight. Coastal work borrows the general ideas of density, rhythm and event atmosphere while using maritime roofs and the game's own coastal palette. Do not reproduce the reference's logos or exact buildings/canopies.

## What to do next

### Immediate: evaluate the accepted AI baseline

Race several rounds without changing settings first:

1. Compare tight bends and straights: rivals should no longer brake for curvature alone.
2. Check repeated draft/pull-out/pass exchanges, including whether a passed rival can stay close or return.
3. Distinguish legitimate following-distance slowing from old corner braking.
4. Watch lane changes and full-speed packs for bunching or cars seeming to overlap.
5. Confirm bank alignment, lap position, restart/pause and Time Trial remain correct.

If rivals still cannot recover, inspect real gap, target speed, traffic cap and available straight length before increasing rubberbanding. Adjust one parameter at a time. Do not change baseline pace, drafting and catch-up together or add teleporting.

### Later, by agreement

- Higher overall speed, then a deliberate handling pass with understeer/oversteer.
- When traction limits are introduced, give both player and AI sensible corner-speed/braking rules. Keep headset motion comfortable.
- Cockpit tachometer and gear indication are desired later, not implemented.
- More distinct track sectors, selective coastal polish, possible useful marina services.
- New downtown circuit inspired by the saved urban references, with its own identity.
- Physical contact, advanced racing lines, more sophisticated opponent behavior only if wanted; not prerequisites for this current working arcade baseline.

### Deferred browser multiplayer port

A three.js port remains a future project. Thomas's other three.js game already runs from GitHub with Create Room → generated room code → friends join. Inspect and reuse that working approach where suitable before choosing networking technology.

Potential first scope: two players, then more; desktop/browser VR; optional AI filling slots; initially allow cars to pass through each other. Continue Unity Quest development first. No port or multiplayer implementation was done here.

## Build/setup lessons retained from earlier work

- Working Meta Quest build profile may have overrides separate from base project settings. Preserve the tested profile and OpenXR setup.
- Optional Meta All-in-One/simulator packages caused previous compiler/duplicate loader problems; do not reinstall them for an ordinary gameplay feature.
- Earlier Gradle/build failures were resolved. Do not apply old build fixes blindly over a working installation.
- If a Windows `.cmd` pauses with “press any key,” pressing a key commonly closes that console. Inspect its output before dismissing it, or launch from an already-open command prompt when diagnosing.
- Make no claim of current APK compilation or measured headset FPS from assistant-side mathematical checks.
- Patches should be concrete, small and reversible; Thomas is comfortable testing them in the headset. Do not repeatedly ask permission for an already agreed small iteration.

## Suggested opening message for the next chat

> Continue Polygon Racing VR from my current working Quest 2 Unity 6000.6.4f1 project. Coastal Style v11 Seaside Paddock is installed, with v10 depth-tested/side-selected gantry lettering, v09 five-red-light race start, v08 seaside crowds, Amber Point cliffs, Sunbay Cove, 4-degree banking, marina and minimap. Rival Pace v02 Arcade Corners is also installed and tested: AI no longer brakes for curvature, matching my current flat-out arcade handling, while existing traffic avoidance, slipstreaming and rubberbanding remain. I like the faster rivals. We are keeping this baseline and testing repeated drafting/overtaking before further tuning. Future speed/understeer/oversteer will need corner braking for both player and AI. Preserve my cockpit/tracking/build settings. The old full project ZIP predates these patches; inspect my latest installed project before coding. Downtown screenshot ideas and a three.js room-code multiplayer port are for later.
