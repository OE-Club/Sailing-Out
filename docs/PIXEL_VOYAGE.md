# Seated Free Sailing Prototype

Open this repository with international Unity **6000.6.1f1**, open `Assets/Scenes/FreeSailing.unity`, and press Play.

## Controls

- Enter or the on-screen button: start/resume.
- A/D or Left/Right: steer.
- W/S or Up/Down: sheet in/out.
- Space: pause/resume. R: reset position and speed. H: show controls.

Player-facing controls, HUD, status messages and buttons use Simplified Chinese. The Sailing Out brand remains unchanged. The HUD uses a bundled static subset of Noto Sans SC with embedded font data; glyph coverage is checked on creation and the actual Game view is inspected for missing glyphs and clipping. The subset includes the current UI text, ASCII controls and required punctuation. See `Assets/Resources/Voyage/FontLicense/` for the original OFL license and source attribution. No system or Unity Editor language settings are changed.

There is no course, buoy target, route guidance, lap/leg state, mandatory mark passage or completion timer. Wind arrows show where air travels. Let the sail out downwind and sheet in closer to the wind. Direct headwind stalls the boat; turn across the wind to recover. This is a compact local arcade approximation, not a full hydrodynamic or multiplayer simulation.

## Seated art and rendering

The AI-generated seated sailor image supplied for this task was imported and its actual pixels inspected. See `docs/ART_PROVENANCE.md` for provenance. It is a transparent 1254x1254 PNG, four 627x627 cells in north/east/south/west order. Source image bytes are preserved. Point filtering, no mipmaps and uncompressed import preserve the pixel style; no script distorts the pose.

The pelvis pivots in bottom-origin Unity coordinates are north `(0.50, 0.345)`, east `(0.43, 0.345)`, south `(0.50, 0.407)` and west `(0.535, 0.38)`. All directions use the same scale derived from the loaded half-height divided by 1.5. The separate `Crew seat` plank uses the same world anchor as the pelvis and renders behind the character. Four real Play Mode screenshots are used to inspect the seat contact, bent legs and hull clearance.

## Ownership and integration

`Assets/Scripts/Sailing/` contains local wind dynamics, input, vessel/crew rendering and HUD. It uses the existing GameSession public lifecycle and InfiniteSeaGenerator grid. The only sea code change is Point filtering. Core public interfaces, Player and Monsters are unchanged.

Upstream PR #5, merge `7b5f6653123d366297c4d08ff8b6f776092fbb3d`, was fetched and inspected. SailboatMovement applies WASD directly to Rigidbody2D velocity, while its Main Camera follows as a child of the Sailboat. FreeSailing therefore remains a separate scene and never loads that driver or camera alongside wind controls. This PR leaves Main and the Player module unchanged; the branch does not merge the upstream baseline locally. The proposed FreeSailing scene stays independent of the teammate scene. Adapting wind forces to the Rigidbody2D controller is a later shared-interface decision.

## Validation

Play Mode regression tests cover repeated start/reset, pause, four seated directions, absence of course objects, and single-session scene reload. The existing Edit Mode tests cover wind/trim behavior and input bounds.

`Sailing Out > Run seated free sailing QA` runs the actual visible scene and writes four seated orientations, sailing and pause screenshots plus results under ignored `Logs/FreeSailingQA/`. Old course screenshots/tests are not evidence for this version. No standalone build or human acceptance claim is made.
