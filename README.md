# Sailing Out

Unity 6.1 2D prototype. The project is intentionally small and split into three gameplay modules:

- `Assets/Scripts/Spawning/` - random object generation
- `Assets/Scripts/Player/` - player input and movement
- `Assets/Scripts/Monsters/` - sea monster detection and chase logic

The shared runtime lives in `Assets/Scripts/Core/`. It owns only the application lifecycle, run state, and cross-module contracts. Gameplay modules should depend on Core, but Core must not depend on gameplay modules.

## Required pull request workflow

Every change must go through a separate branch and a pull request targeting `main`, including fixes, documentation, and configuration. Merge through GitHub's PR merge flow.

Before starting a task, run `git fetch origin`. Create a new task branch from the latest `origin/main`; sync existing task branches with the latest fetched `origin/main` as integration requires. Prefer small commits and frequent integration through PRs.

After cloning, review `scripts/install-git-hooks.sh` and `.githooks/` from a trusted checkout, then install the hooks once per clone:

```bash
sh scripts/install-git-hooks.sh
```

The installer copies the reviewed hooks into the Git common metadata directory (`sailing-out-hooks` inside the directory reported by `git rev-parse --path-format=absolute --git-common-dir`) and configures an absolute `core.hooksPath`. The installed hooks are shared by linked worktrees. Switching branches or editing tracked `.githooks/` files does not replace the executable hooks. Install updates only after reviewing both the installer and hook changes; never configure `core.hooksPath` to execute the branch-controlled `.githooks/` directory.

The hooks block local updates to `main`, including commits, cherry-picks, merge commits, and fast-forward merges. They also reject any push whose destination is `main` (including explicit refspecs, force pushes, deletion, and multi-ref pushes). Use `origin/main` to start and sync task branches; local `main` remains protected. Create a task branch before changing files; Codex branches use `codex/<task>`. Do not bypass the hooks.

Install Git LFS and run `git lfs install --skip-repo` to configure its filters without replacing the reviewed hooks. The installed pre-push hook passes allowed pushes to `git lfs pre-push`.

GitHub also enforces this workflow through the active `Require pull requests for main` ruleset on the default branch (`main`), with no bypass actors. Direct pushes, force pushes, and branch deletion are blocked. A PR is required; an approving review is not required. Local hooks must be enabled separately in each clone.

`AGENTS.md` is the standard entry point for agents and includes the Chinese code review requirement. Read `agents.me` for the full ownership, Unity, and integration rules.

Run the hook regression checks with `python3 -m unittest discover -s tests -p 'test_git_hooks.py' -v`. They use temporary repositories and local remotes.

GitHub Actions runs metadata/configuration validation and repository regression
tests on pull requests and `main`. These are repository checks, not Unity
compilation or Edit/Play tests. See [CI scope and Unity licensing requirements](docs/CI.md).

## Open and run

Open the folder with Unity `6000.6.1f1`. Open `Assets/Scenes/FreeSailing.unity` and press Play. This isolated free sailing prototype reuses `GameBootstrap`, `GameSession` and the infinite sea, with a seated crew member, wind, steering and sail trim. There is no course or timer. See `docs/PIXEL_VOYAGE.md` for controls, scope and validation.

## Module integration

Use `GameSession.Instance` for run lifecycle and subscribe to `StateChanged` when a module needs to react to pause, restart, or game over. Use the interfaces in `Assets/Scripts/Core/Contracts/` for interactions between modules. Do not reach into another module's private fields.

The self-contained wind sailing prototype lives in `Assets/Scripts/Sailing/`. It preserves Player/Monsters ownership. Upstream PR #5 adds a separate Rigidbody2D movement and camera entry in Main; FreeSailing does not load that controller alongside its wind controls.

## Infinite sea

`InfiniteSeaGenerator` in `Assets/Scripts/Spawning/` creates a procedural 2D water tile and keeps a reusable grid centered around the main camera (or an explicitly assigned target). It defaults to a 7x5 grid and repositions existing tiles when the target crosses a tile boundary, so movement does not allocate an unbounded number of GameObjects. Tune the grid, tile size, colors, and wave shape on the `Infinite Sea` object in `Main.unity`.
