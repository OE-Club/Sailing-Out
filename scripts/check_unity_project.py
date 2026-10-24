"""Validate tracked Unity metadata/configuration without launching the Editor.

This does not compile C#, resolve UPM packages, or execute Unity tests.
"""

import argparse
import json
from pathlib import Path
import re
import subprocess
import sys


GUID = re.compile(r"^[0-9a-f]{32}$")
REFERENCE = re.compile(r"\bguid:\s*([0-9a-fA-F]{32})\b")
SERIALIZED = {".unity", ".prefab", ".asset", ".mat", ".anim", ".controller", ".overrideController"}


def tracked_files(root):
    return set(subprocess.check_output(
        ["git", "ls-files", "-z"], cwd=root
    ).decode("utf-8").rstrip("\0").split("\0")) - {""}


def build_scene_entries(build):
    """Enumerate the Unity m_Scenes list before validating any scalar values."""
    entries = []
    section_indent = None
    for line in build.splitlines():
        stripped = line.strip()
        indent = len(line) - len(line.lstrip())
        if section_indent is None:
            if stripped.startswith("m_Scenes:"):
                section_indent = indent
                inline = stripped.partition(":")[2].strip()
                if inline:
                    return [] if inline == "[]" else [[inline]]
            continue
        if not stripped or stripped.startswith("#"):
            continue
        if indent < section_indent or (indent == section_indent and not stripped.startswith("-")):
            break
        if stripped.startswith("-"):
            entries.append([stripped[1:].strip()])
        elif entries:
            entries[-1].append(stripped)
        else:
            entries.append([stripped]) # Malformed entries must also reach validation.
    return entries


def check_project(root, files=None):
    root = Path(root)
    files = tracked_files(root) if files is None else set(files)
    errors = []
    assets = {p for p in files if p.startswith("Assets/")}
    folders = {p.as_posix() for name in assets for p in Path(name).parents if p.as_posix() != "."}
    guids = {}
    asset_guids = {}

    def text(name):
        path = root / name
        if path.is_symlink():
            errors.append(f"{name}: symlinks are not supported by this checker")
            return ""
        try:
            return path.read_text(encoding="utf-8-sig")
        except (OSError, UnicodeError) as exc:
            errors.append(f"{name}: {exc}")
            return ""

    def read_json(name):
        try:
            value = json.loads(text(name))
            if not isinstance(value, dict):
                raise ValueError("expected a JSON object")
            return value
        except ValueError as exc:
            errors.append(f"{name}: {exc}")
            return {}

    for name in sorted(assets | (folders - {"Assets"})):
        if not name.endswith(".meta") and name + ".meta" not in assets:
            errors.append(f"{name}: missing tracked .meta")
    for name in sorted(n for n in assets if n.endswith(".meta")):
        target = name[:-5]
        if target not in assets and target not in folders:
            errors.append(f"{name}: orphan .meta")
        data = text(name)
        matches = re.findall(r"^guid:\s*(\S+)\s*$", data, re.MULTILINE)
        if len(matches) != 1 or not GUID.fullmatch(matches[0]):
            errors.append(f"{name}: expected exactly one lowercase 32-character GUID")
            continue
        guid = matches[0]
        if guid in guids:
            errors.append(f"{name}: duplicate GUID also used by {guids[guid]}")
        guids[guid] = name
        asset_guids[target] = guid
        folder_flag = bool(re.search(r"^folderAsset: yes\s*$", data, re.MULTILINE))
        if folder_flag != (target in folders):
            errors.append(f"{name}: folderAsset flag disagrees with tracked asset type")

    # Embedded package scripts and assets can be referenced by scenes under Assets.
    for name in sorted(n for n in files if n.startswith("Packages/") and n.endswith(".meta")):
        data = text(name)
        matches = re.findall(r"^guid:\s*(\S+)\s*$", data, re.MULTILINE)
        if len(matches) != 1 or not GUID.fullmatch(matches[0]):
            errors.append(f"{name}: expected exactly one lowercase 32-character GUID")
            continue
        guid = matches[0]
        if guid in guids:
            errors.append(f"{name}: duplicate GUID also used by {guids[guid]}")
        guids[guid] = name

    for name in sorted(n for n in files if Path(n).suffix in SERIALIZED):
        for guid in REFERENCE.findall(text(name)):
            # Unity's null and built-in resource GUIDs do not refer to project .meta files.
            builtin = guid == "0" * 32 or re.fullmatch(r"0{16}[ef]0{15}", guid)
            if not builtin and guid not in guids:
                errors.append(f"{name}: unresolved GUID {guid}")

    version = text("ProjectSettings/ProjectVersion.txt")
    editor = re.search(r"^m_EditorVersion: (\d+\.\d+\.\d+[abfp]\d+)\s*$", version, re.MULTILINE)
    revision = re.search(r"^m_EditorVersionWithRevision: (\S+) \(([0-9a-f]{12})\)\s*$", version, re.MULTILINE)
    if not editor or not revision or editor[1] != revision[1]:
        errors.append("ProjectSettings/ProjectVersion.txt: editor version/revision must be pinned and agree")

    manifest = read_json("Packages/manifest.json").get("dependencies", {})
    locked = read_json("Packages/packages-lock.json").get("dependencies", {})
    if not isinstance(manifest, dict) or not isinstance(locked, dict):
        errors.append("Packages: dependencies must be JSON objects")
        manifest, locked = {}, {}
    for name, version in sorted(manifest.items()):
        entry = locked.get(name, {})
        if not isinstance(entry, dict) or entry.get("version") != version or entry.get("depth") != 0:
            errors.append(f"Packages: {name} manifest version must match its direct lock entry")
    if "com.unity.test-framework" not in manifest:
        errors.append("Packages: Unity Test Framework must be declared")

    assemblies = {}
    for name in sorted(n for n in assets if n.endswith(".asmdef")):
        definition = read_json(name)
        assembly = definition.get("name")
        if not isinstance(assembly, str) or not assembly or assembly in assemblies:
            errors.append(f"{name}: assembly name must be nonempty and unique")
        else:
            assemblies[assembly] = (name, definition)
    for name, definition in assemblies.values():
        refs = definition.get("references", [])
        if not isinstance(refs, list) or not all(isinstance(ref, str) for ref in refs):
            errors.append(f"{name}: assembly references must be a list of strings")
            continue
        for ref in refs:
            if ref.startswith("SailingOut.") and ref not in assemblies:
                errors.append(f"{name}: unresolved project assembly {ref}")
            if ref == "Unity.InputSystem" and "com.unity.inputsystem" not in manifest:
                errors.append(f"{name}: Unity.InputSystem requires its package")
        if name.startswith("Assets/Tests/"):
            if "TestAssemblies" not in definition.get("optionalUnityReferences", []):
                errors.append(f"{name}: test assembly must opt into TestAssemblies")
            if "/EditMode/" in name and definition.get("includePlatforms") != ["Editor"]:
                errors.append(f"{name}: Edit Mode tests must be Editor-only")
            if "/PlayMode/" in name and definition.get("includePlatforms", []):
                errors.append(f"{name}: Play Mode tests must not be Editor-only")
    for mode in ("EditMode", "PlayMode"):
        directory = f"Assets/Tests/{mode}"
        if not any(p.startswith(directory + "/") and p.endswith(".cs") for p in assets):
            errors.append(f"{directory}: no tracked test sources")
        valid_assembly = any(
            Path(name).parent.as_posix() == directory
            and isinstance(definition.get("optionalUnityReferences"), list)
            and "TestAssemblies" in definition["optionalUnityReferences"]
            and definition.get("includePlatforms", []) == (["Editor"] if mode == "EditMode" else [])
            for name, definition in assemblies.values()
        )
        if not valid_assembly:
            errors.append(f"{directory}: requires a valid test .asmdef at the mode directory root")

    build = text("ProjectSettings/EditorBuildSettings.asset")
    scenes = build_scene_entries(build)
    if not scenes:
        errors.append("EditorBuildSettings: no scene entries found")
    for index, entry in enumerate(scenes, 1):
        fields = {}
        for line in entry:
            key, separator, value = line.partition(":")
            if separator:
                if key in fields:
                    errors.append(f"EditorBuildSettings: scene {index} has duplicate {key}")
                fields[key] = value.strip()
        path, guid = fields.get("path", ""), fields.get("guid", "")
        if (fields.get("enabled") not in {"0", "1"} or not path.startswith("Assets/")
                or not path.endswith(".unity") or not GUID.fullmatch(guid)):
            errors.append(f"EditorBuildSettings: scene {index} has missing or invalid enabled/path/GUID fields")
            continue
        if path not in assets or asset_guids.get(path) != guid:
            errors.append(f"EditorBuildSettings: scene path/GUID mismatch for {path}")
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    try:
        errors = check_project(args.root)
    except (OSError, subprocess.CalledProcessError) as exc:
        print(f"Repository check could not run: {exc}", file=sys.stderr)
        return 1
    for error in errors:
        print(f"ERROR: {error}", file=sys.stderr)
    if errors:
        return 1
    print("Tracked Unity metadata/configuration checks passed (Unity compilation/tests were not run).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
