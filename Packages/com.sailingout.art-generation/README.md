# Sailing Out Art Generation package

This embedded Unity package supplies the runtime portion of the art/decoration module:

- deterministic segment randomization from a run seed;
- continuous positions with minimum separation checks;
- a safe lane at least as wide as the player collider;
- reef, supply, buoy, and seabird category rules;
- trigger-based buoy contact that slows the demo boat to half speed for 0.5 seconds;
- buoy contact also pushes the demo boat to a random horizontal side and removes half a heart;
- reef contact removes half a heart; the demo HUD starts each run with three hearts;
- collecting a supply restores half a heart up to full health, then hides the supply even if health was already full;
- reaching zero hearts stops the boat and displays a full-screen red death screen with a restart button;
- W activates one of three 3-second, 2x speed boosts per voyage; green lightning charges appear in the upper-right HUD;
- non-blocking floating-decoration variants and tiled water/foam background;
- pooled reuse and lifecycle callbacks;
- distance-based generation stages;
- editor tooling that converts named pixel sprites to variants, animations, Prefabs, definitions, and a demo scene.

## Build the art package

1. Open `Assets/SailingOut/Generated/Demo/SailingOutGenerationDemo.unity` and press Play.
2. Use A/D to steer, Space for guided navigation, R to replay, N for a new seed, F1 for diagnostics.
3. Edit the included original PNGs, preserving their `.meta` files and naming conventions.
4. Use **Sailing Out > Rebuild Included Demo** to regenerate Prefabs, animations and configs.
5. The custom builder window also supports selecting alternative source/output folders.

The release includes 30 original pixel frames, 15 sprite sheets, water/foam textures, a demo boat, 15 Prefabs and 9 animation clips. The user selected directly authored pixel art because this session has no image generation tool. AI concept prompts remain available for future art iteration.

## Runtime integration

Add `SpawnManager` to a scene object and assign:

- the generated `GenerationProfile`;
- an orthographic camera;
- the player's `Collider2D`.

The manager exposes `Initialize(int seed)`, `EnsureAhead(float playerY, float playerX)`, `RecycleBehind(float playerY)`, and `ClearAndReturnToPool()`. It does not modify player movement or gameplay effects.

Optional `Configure(profile, camera, collider, automatic)` assigns its references. Initialize freezes the run's camera size, lane origin and boat bounds. Keep those settings and start position identical for seed reproduction; the field stays fixed horizontally as the boat steers. Default caps are 256 active + 256 pooled, while typical counts are much smaller. Lowering caps enough to truncate a segment also makes replay dependent on the caller's update cadence. Full Chinese integration/art guides live in `Assets/SailingOut/Documentation/`.

Only built-in Unity modules are required; the isolated demo uses IMGUI key events, compatible with either project input mode. For a new project, import the delivered `.unitypackage`. Do not import it alongside this embedded package, since its relocated module assemblies would be duplicated.
