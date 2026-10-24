"""Exercise Git policy hooks in disposable repositories, without network access."""

import os
from pathlib import Path
import shlex
import shutil
import subprocess
import tempfile
import unittest


PROJECT_ROOT = Path(__file__).resolve().parents[1]
HOOK_NAMES = ("pre-commit", "pre-merge-commit", "pre-push", "reference-transaction")


class GitHooksTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="sailing out hooks ")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.repo = self.root / "checkout"
        self.repo.mkdir()
        self.env = {key: value for key, value in os.environ.items() if not key.startswith("GIT_")}
        self.env.update(
            GIT_CONFIG_NOSYSTEM="1",
            GIT_CONFIG_GLOBAL=os.devnull,
            GIT_TERMINAL_PROMPT="0",
        )
        self.git("init", "--quiet", "--initial-branch=main")
        self.git("config", "user.name", "Hook Test")
        self.git("config", "user.email", "hooks@example.invalid")
        shutil.copytree(PROJECT_ROOT / ".githooks", self.repo / ".githooks")
        (self.repo / "scripts").mkdir()
        shutil.copy2(PROJECT_ROOT / "scripts/install-git-hooks.sh", self.repo / "scripts")
        self.git("add", ".")
        self.git("commit", "--quiet", "-m", "fixture base")
        self.base = self.git("rev-parse", "HEAD").stdout.strip()
        self.git("switch", "--quiet", "-c", "codex/task")
        (self.repo / "feature.txt").write_text("task change\n")
        self.git("add", "feature.txt")
        self.git("commit", "--quiet", "-m", "fixture feature")
        self.feature = self.git("rev-parse", "HEAD").stdout.strip()
        self.git("switch", "--quiet", "main")
        self.run_command("sh", "scripts/install-git-hooks.sh")
        self.hooks_path = Path(self.git("config", "--local", "--get", "core.hooksPath").stdout.strip())

        # Only Git LFS's transport is stubbed; Git and all installed hooks run normally.
        self.lfs_args = self.repo / "lfs-args"
        self.lfs_input = self.repo / "lfs-input"
        fake_bin = self.root / "bin"
        fake_bin.mkdir()
        lfs = fake_bin / "git-lfs"
        lfs.write_text(
            '#!/bin/sh\nset -eu\nprintf "%s\\n" "$@" > "$SAILING_OUT_TEST_LFS_ARGS"\n'
            'cat > "$SAILING_OUT_TEST_LFS_INPUT"\n'
        )
        lfs.chmod(0o755)
        self.env.update(
            PATH=str(fake_bin) + os.pathsep + self.env["PATH"],
            SAILING_OUT_TEST_LFS_ARGS=self.lfs_args.name,
            SAILING_OUT_TEST_LFS_INPUT=self.lfs_input.name,
        )

    def run_command(self, *args, cwd=None, check=True, input=None):
        result = subprocess.run(
            args, cwd=cwd or self.repo, env=self.env, text=True,
            input=input, capture_output=True,
        )
        if check:
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        return result

    def git(self, *args, **kwargs):
        return self.run_command("git", *args, **kwargs)

    def assert_blocked(self, *args):
        result = self.git(*args, check=False)
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("Blocked:", result.stderr)
        self.assertEqual(self.git("rev-parse", "main").stdout.strip(), self.base)

    def add_remote(self):
        remote = self.root / "remote.git"
        self.git("clone", "--quiet", "--bare", str(self.repo), str(remote))
        self.git("remote", "add", "origin", str(remote))
        return remote

    def test_installer_uses_executable_copies_in_git_metadata(self):
        self.assertTrue(self.hooks_path.is_absolute())
        self.assertEqual(self.hooks_path.parent.resolve(), (self.repo / ".git").resolve())
        for name in HOOK_NAMES:
            with self.subTest(hook=name):
                installed = self.hooks_path / name
                self.assertEqual(installed.read_bytes(), (self.repo / ".githooks" / name).read_bytes())
                self.assertTrue(os.access(installed, os.X_OK))
                self.run_command("sh", "-n", str(installed))
        self.run_command("sh", "scripts/install-git-hooks.sh")
        self.assertEqual(self.git("config", "--get", "core.hooksPath").stdout.strip(), self.hooks_path.as_posix())

    def test_commit_on_main_is_blocked(self):
        (self.repo / "blocked.txt").write_text("should not commit\n")
        self.git("add", "blocked.txt")
        self.assert_blocked("commit", "-m", "must be blocked")

    def test_cherry_pick_on_main_is_blocked(self):
        self.assert_blocked("cherry-pick", self.feature)

    def test_fast_forward_merge_on_main_is_blocked(self):
        self.assert_blocked("merge", "--ff-only", "codex/task")

    def test_merge_commit_on_main_is_blocked(self):
        self.assert_blocked("merge", "--no-ff", "codex/task", "-m", "must be blocked")

    def test_main_cannot_be_updated_from_another_branch(self):
        self.git("switch", "--quiet", "codex/task")
        self.assert_blocked("update-ref", "refs/heads/main", self.feature)

    def test_task_branch_commits_cherry_picks_and_merges_are_allowed(self):
        self.git("switch", "--quiet", "codex/task")
        (self.repo / "next.txt").write_text("next task change\n")
        self.git("add", "next.txt")
        self.git("commit", "--quiet", "-m", "allowed task commit")
        for operation in ("cherry-pick", "fast-forward", "merge-commit"):
            with self.subTest(operation=operation):
                self.git("switch", "--quiet", "-c", "codex/" + operation, self.base)
                if operation == "cherry-pick":
                    self.git("cherry-pick", self.feature)
                elif operation == "fast-forward":
                    self.git("merge", "--ff-only", self.feature)
                else:
                    self.git("merge", "--no-ff", self.feature, "-m", "allowed task merge")
                self.assertTrue((self.repo / "feature.txt").exists())

    def test_fetch_updates_origin_main_without_updating_local_main(self):
        remote = self.add_remote()
        # Simulate a server-side PR merge in the disposable remote.
        self.git("--git-dir=" + str(remote), "update-ref", "refs/heads/main", self.feature)
        self.git("fetch", "origin")
        self.assertEqual(self.git("rev-parse", "origin/main").stdout.strip(), self.feature)
        self.assertEqual(self.git("rev-parse", "main").stdout.strip(), self.base)

    def test_all_pushes_targeting_main_are_blocked(self):
        remote = self.add_remote()
        self.git("switch", "--quiet", "codex/task")
        pushes = (
            ("HEAD:refs/heads/main",),
            ("--force", "HEAD:refs/heads/main"),
            (":refs/heads/main",),
            ("HEAD:refs/heads/codex/other", "HEAD:refs/heads/main"),
        )
        for refspecs in pushes:
            with self.subTest(refspecs=refspecs):
                self.assert_blocked("push", "origin", *refspecs)
                self.assertFalse(self.lfs_args.exists())
        self.assertEqual(
            self.git("--git-dir=" + str(remote), "rev-parse", "main").stdout.strip(), self.base
        )
        other = self.git("--git-dir=" + str(remote), "show-ref", "--verify", "refs/heads/codex/other", check=False)
        self.assertNotEqual(other.returncode, 0)

    def test_task_push_is_allowed_and_lfs_receives_original_updates(self):
        remote = self.add_remote()
        self.git("switch", "--quiet", "codex/task")
        self.git("push", "origin", "HEAD:refs/heads/codex/allowed")
        self.assertEqual(
            self.git("--git-dir=" + str(remote), "rev-parse", "codex/allowed").stdout.strip(), self.feature
        )

        if os.name == "nt":
            self.skipTest("Windows cannot run the POSIX Git LFS transport stub; Linux CI covers forwarding")

        update = f"HEAD {self.feature} refs/heads/codex/allowed {'0' * 40}\n"
        resolved_lfs = self.run_command("sh", "-c", "command -v git-lfs")
        self.assertTrue(
            resolved_lfs.stdout.strip().replace("\\", "/").endswith("/bin/git-lfs"),
            resolved_lfs.stdout + resolved_lfs.stderr,
        )
        self.run_command(
            "sh", str(self.hooks_path / "pre-push"), "origin", str(remote), input=update
        )
        self.assertEqual(self.lfs_args.read_text().splitlines(), ["pre-push", "origin", str(remote)])
        fields = self.lfs_input.read_text().split()
        self.assertEqual(fields, update.split())

    def test_branch_controlled_hook_changes_are_not_executed(self):
        self.add_remote()
        self.git("switch", "--quiet", "codex/task")
        marker = self.root / "untrusted-hook-ran"
        for name in HOOK_NAMES:
            (self.repo / ".githooks" / name).write_text(
                "#!/bin/sh\nprintf tampered > " + shlex.quote(str(marker)) + "\nexit 0\n"
            )
        self.git("add", ".githooks")
        self.git("commit", "--quiet", "-m", "fixture untrusted branch hooks")
        self.git("push", "origin", "HEAD:refs/heads/codex/tampered")
        self.assert_blocked("push", "origin", "HEAD:refs/heads/main")
        self.assert_blocked("update-ref", "refs/heads/main", "HEAD")
        self.assertFalse(marker.exists())

    def test_linked_worktrees_share_the_trusted_hooks(self):
        linked = self.root / "linked checkout"
        self.git("worktree", "add", "--quiet", "-b", "codex/linked", str(linked), "codex/task")
        self.assertEqual(
            self.git("config", "--get", "core.hooksPath", cwd=linked).stdout.strip(), self.hooks_path.as_posix()
        )
        marker = self.root / "linked-hook-ran"
        (linked / ".githooks/pre-commit").write_text(
            "#!/bin/sh\nprintf tampered > " + shlex.quote(str(marker)) + "\nexit 0\n"
        )
        self.git("add", ".githooks/pre-commit", cwd=linked)
        self.git("commit", "--quiet", "-m", "allowed worktree task commit", cwd=linked)
        self.assertFalse(marker.exists())


if __name__ == "__main__":
    unittest.main()
