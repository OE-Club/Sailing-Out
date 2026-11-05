# Agent instructions

Read `agents.me` and `README.md` before editing. `agents.me` contains the project's module ownership, Unity, and integration rules.

## Mandatory pull request workflow

- Every change must use a separate branch and a pull request targeting `main`, including small fixes, documentation, and configuration changes.
- Never commit, merge locally, or push directly to `main`. Create a task branch first; Codex branches use the `codex/` prefix.
- Before committing or pushing, ensure the reviewed repository hooks are installed and enabled. For initial setup or reviewed hook updates, run `sh scripts/install-git-hooks.sh` only from a checkout whose installer and hook sources you have reviewed. It copies hooks into Git's common metadata directory and sets an absolute `core.hooksPath`; keep that installed path enabled across branches and worktrees.
- Push the task branch, open a PR, and include the PR link and the validation actually performed in the completion report.
- Integrate changes into `main` only through GitHub's PR merge flow.
- Do not bypass the hooks with `--no-verify`, an alternate hooks path, or another tool/API that directly updates `main`.
- Preserve unrelated local changes and stage only files belonging to the task.

## Code reviews

代码审查必须使用中文撰写，包括审查摘要、问题说明和修改建议；代码标识符、文件路径和命令保持原样。

PR 审批与合并须遵守 [SO 的 PR 审批与合并规则](docs/PR_APPROVALS.md)：Codex Review 通过、CI 通过且没有合并冲突。安全审查卡住的例外不能绕过 GitHub 保护。cchhh 的 GitHub 账号已确认为 `ccchh4rr`；文档授权不构成给助手的永久审批许可，每次提交 `APPROVE` 仍需用户针对当次 PR 和对应提交的明确确认。
