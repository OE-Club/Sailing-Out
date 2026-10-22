# Continuous integration

## Enabled checks

`Repository checks` runs for pull requests targeting `main`, pushes to `main`,
and manual dispatches. Its job is explicitly named **Repository checks (not
Unity tests)**. It performs real checks and fails when a check fails:

- All GitHub Actions workflows' syntax with actionlint 1.7.12, downloaded from its
  official release and verified against a pinned SHA-256 digest.
- Tracked asset/folder `.meta` coverage, orphan metadata, valid unique GUIDs,
  serialized GUID references and build-scene path/GUID agreement.
- The editor version/revision pin, direct package manifest/lock agreement,
  project assembly references and Edit/Play test-assembly structure.
- Python validator regression tests and the existing Git hook regression suite,
  which runs Git operations only in disposable local repositories.

Run the same repository checks locally from the repository root:

```sh
python3 scripts/check_unity_project.py
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests -p 'test_*.py' -v
```

Only Git-tracked Unity assets are checked; an untracked `.meta` cannot hide a
missing committed file. The current serialized asset references are all local
or Unity built-ins. A new package-owned GUID will fail as unresolved until its
source and package-aware validation are explicitly added; unknown GUIDs are
not silently ignored. Importer behavior, subasset file IDs and C# compilation
remain the Editor's responsibility.

Each test mode requires a valid test `.asmdef` at its directory root, and every
`m_Scenes` list entry is validated, including disabled or malformed entries.
Actionlint discovers all `.yml` and `.yaml` workflows instead of checking only
the CI workflow itself.

## Security and cost

The workflow uses a standard `ubuntu-24.04` GitHub-hosted runner, a five-minute
timeout and cancellation of superseded runs. The checkout action is pinned to
its full commit SHA, credentials are not persisted, and `GITHUB_TOKEN` has only
`contents: read`. There is no `pull_request_target`, secret access, cache,
artifact upload, deployment, self-hosted runner or paid service setup.

This repository was public when CI was added. GitHub documents standard hosted
runner usage in public repositories as free. Reassess billing before making the
repository private or changing runner type or storage behavior. Repository and
organization Actions settings were inspected without modification: Actions
were enabled and default workflow permissions were read-only.

## Unity compilation and tests are not yet cloud CI

A green repository-check job does **not** mean that Unity compiled the project
or ran the C# tests in `Assets/Tests/`. There is no skipped placeholder Unity
job that can appear to pass without executing tests.

The project pins Unity **6000.6.1f1**, revision **7efac9f6c10e**. On 2026-10-22,
Docker Hub's public tag API confirmed the GameCI image
`unityci/editor:ubuntu-6000.6.1f1-base-3.2.2` for `linux/amd64`, with digest:

```text
sha256:2197a718c75ba71d6d9a05cfdfbce31cc401113f530963ac789160dffc96763d
```

That verifies image metadata, not a successful image launch or test run. The
GitHub settings page showed no repository secrets, environment secrets, or
organization secrets available to this repository. In particular, there is no
configured cloud Unity licensing route. A local Mac Editor license does not
establish licensing for a separate Linux cloud runner.

Before adding a Unity job, obtain explicit approval for a license type and
secure activation method supported by Unity and GameCI. GameCI documents
license/credential secrets or a licensing server, depending on the license
type. Unity 6.6's official documentation says command-line/manual activation
does not apply to Unity Personal; Personal activation uses Unity Hub. GameCI's
Personal-license examples must not be treated as proof that a new Personal
license can be activated headlessly for this Editor. Resolve that compatibility
and the intended license entitlement before provisioning any Unity CI secrets.
Do not copy the Mac license, account password or credentials into this
repository, upload them as artifacts, or provision persistent access as part of
this CI change.

Once licensing is approved, add a separately reviewed job that pins the exact
Editor image and test-runner action, compiles the project and actually executes
both Edit Mode and Play Mode tests. Verify Linux graphics/font support for the
seated sprites and Chinese HUD, and actual assembly-reload tests when present.
Treat a missing license, missing XML results, zero discovered tests or a failing
test as failure. Keep secret-bearing runs on reviewed trusted code; fork PRs
must not gain license secrets. No standalone player build or Unity cloud test
has been claimed by this workflow.

## Official references

- [GitHub Actions billing and usage](https://docs.github.com/en/actions/concepts/billing-and-usage)
- [GitHub Actions secure use](https://docs.github.com/en/actions/reference/security/secure-use)
- [GameCI test runner and licensing](https://game.ci/docs/github/test-runner/)
- [GameCI image versions](https://game.ci/docs/docker/versions/)
- [Exact-version Docker Hub metadata](https://hub.docker.com/v2/repositories/unityci/editor/tags?page_size=100&name=6000.6.1f1)
- [Unity 6.6 command-line interface](https://docs.unity3d.com/6000.6/Documentation/Manual/CommandLineArguments.html)
- [Unity 6.6 license management and Personal limitations](https://docs.unity3d.com/6000.6/Documentation/Manual/ManagingYourUnityLicense.html)
- [Unity 6.6 manual activation eligibility](https://docs.unity3d.com/6000.6/Documentation/Manual/ManualActivationGuide.html)
