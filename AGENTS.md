# Agent instructions

Read `agents.me` and `README.md` before editing. `agents.me` contains the project's module ownership, Unity, and integration rules.

## Mandatory pull request workflow

- Every change must use a separate branch and a pull request targeting `main`, including small fixes, documentation, and configuration changes.
- Never commit, merge locally, or push directly to `main`. Create a task branch first; Codex branches use the `codex/` prefix.
- Ensure the repository hooks are enabled before committing or pushing. For a new clone, run `git config --local core.hooksPath .githooks`.
- Push the task branch, open a PR, and include the PR link and the validation actually performed in the completion report.
- Integrate changes into `main` only through GitHub's PR merge flow.
- Do not bypass the hooks with `--no-verify`, an alternate hooks path, or another tool/API that directly updates `main`.
- Preserve unrelated local changes and stage only files belonging to the task.
