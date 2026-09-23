# Sailing Out

Unity 6.1 2D prototype. The project is intentionally small and split into three gameplay modules:

- `Assets/Scripts/Spawning/` - random object generation
- `Assets/Scripts/Player/` - player input and movement
- `Assets/Scripts/Monsters/` - sea monster detection and chase logic

The shared runtime lives in `Assets/Scripts/Core/`. It owns only the application lifecycle, run state, and cross-module contracts. Gameplay modules should depend on Core, but Core must not depend on gameplay modules.

## Open and run

Open the folder with Unity `6000.6.1f1`. Open `Assets/Scenes/Main.unity` and press Play. The scene contains the `GameBootstrap` and `GameSession` runtime objects; the scene is deliberately empty so each module owner can add their own content without merge conflicts.

## Module integration

Use `GameSession.Instance` for run lifecycle and subscribe to `StateChanged` when a module needs to react to pause, restart, or game over. Use the interfaces in `Assets/Scripts/Core/Contracts/` for interactions between modules. Do not reach into another module's private fields.

The starter framework does not include gameplay behavior, prefabs, art, or input bindings. Those belong to the module owners listed in `agents.me`.
