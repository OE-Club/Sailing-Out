"""Reject damaged metadata/configuration in disposable fixtures."""

import importlib.util
import json
from pathlib import Path
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("check_unity_project", ROOT / "scripts/check_unity_project.py")
CHECKER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(CHECKER)


class UnityProjectChecksTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.files = set()
        self.write("ProjectSettings/ProjectVersion.txt", "m_EditorVersion: 6000.6.1f1\nm_EditorVersionWithRevision: 6000.6.1f1 (7efac9f6c10e)\n")
        packages = {"com.unity.test-framework": "1.8.0"}
        self.write("Packages/manifest.json", json.dumps({"dependencies": packages}))
        self.write("Packages/packages-lock.json", json.dumps({"dependencies": {
            name: {"version": version, "depth": 0} for name, version in packages.items()
        }}))
        for i, path in enumerate(("Scenes", "Tests", "Tests/EditMode", "Tests/PlayMode"), 1):
            self.meta("Assets/" + path, f"{i:032x}", True)
        self.scene_guid = "a" * 32
        self.write("Assets/Scenes/Test.unity", "%YAML 1.1\nguid: " + "0" * 32)
        self.meta("Assets/Scenes/Test.unity", self.scene_guid)
        self.write("ProjectSettings/EditorBuildSettings.asset", "m_Scenes:\n- enabled: 1\n  path: Assets/Scenes/Test.unity\n  guid: " + self.scene_guid)
        for i, mode in enumerate(("EditMode", "PlayMode"), 10):
            asm = f"Assets/Tests/{mode}/Tests.asmdef"
            self.write(asm, json.dumps({"name": f"SailingOut.{mode}", "optionalUnityReferences": ["TestAssemblies"], "includePlatforms": ["Editor"] if mode == "EditMode" else []}))
            self.meta(asm, f"{i:032x}")
            self.write(f"Assets/Tests/{mode}/Example.cs", "// Fixture source")
            self.meta(f"Assets/Tests/{mode}/Example.cs", f"{i+10:032x}")

    def write(self, name, data):
        path = self.root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(data)
        self.files.add(name)

    def meta(self, name, guid, folder=False):
        self.write(name + ".meta", f"fileFormatVersion: 2\nguid: {guid}\n" + ("folderAsset: yes\n" if folder else ""))

    def errors(self):
        return CHECKER.check_project(self.root, self.files)

    def test_valid_project(self):
        self.assertEqual(self.errors(), [])

    def test_missing_meta_is_not_hidden_by_untracked_file(self):
        self.files.remove("Assets/Scenes/Test.unity.meta")
        self.assertTrue(any("missing tracked .meta" in e for e in self.errors()))

    def test_duplicate_and_orphan_guids(self):
        self.meta("Assets/Scenes/Missing.unity", self.scene_guid)
        errors = self.errors()
        self.assertTrue(any("duplicate GUID" in e for e in errors))
        self.assertTrue(any("orphan .meta" in e for e in errors))

    def test_broken_reference_and_build_scene_guid(self):
        self.write("Assets/Scenes/Test.unity", "guid: " + "b" * 32)
        self.write("ProjectSettings/EditorBuildSettings.asset", "path: Assets/Scenes/Test.unity\n guid: " + "c" * 32)
        errors = self.errors()
        self.assertTrue(any("unresolved GUID" in e for e in errors))
        self.assertTrue(any("scene path/GUID mismatch" in e for e in errors))

    def test_stale_lock_and_invalid_json(self):
        self.write("Packages/packages-lock.json", '{"dependencies": {}}')
        self.assertTrue(any("direct lock entry" in e for e in self.errors()))
        self.write("Packages/manifest.json", "{broken")
        self.assertTrue(any("manifest.json" in e for e in self.errors()))

    def test_editor_pin_and_test_assembly_boundaries(self):
        self.write("ProjectSettings/ProjectVersion.txt", "m_EditorVersion: latest")
        self.write("Assets/Tests/EditMode/Tests.asmdef", json.dumps({"name": "SailingOut.EditMode", "references": ["SailingOut.Missing"]}))
        errors = self.errors()
        for expected in ("version/revision", "unresolved project assembly", "TestAssemblies", "Editor-only"):
            self.assertTrue(any(expected in e for e in errors), expected)


if __name__ == "__main__":
    unittest.main()
