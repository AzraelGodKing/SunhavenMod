#!/usr/bin/env python3
"""
Ensure docs/versions.json "version" matches PLUGIN_VERSION and thunderstore/manifest.json
for every mod. Run in CI (setup job) before build; run locally after editing versions or plugins.

See docs/VERSION_AND_RELEASE.md
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
VERSIONS_PATH = REPO / "docs" / "versions.json"
MOD_MATRIX_PATH = REPO / "scripts" / "mod-matrix.json"
# utf-8-sig strips UTF-8 BOM if present (some editors save JSON with BOM).
_READ_ENCODING = "utf-8-sig"

PLUGIN_VER_RE = re.compile(r'PLUGIN_VERSION\s*=\s*"([^"]*)"')

MANIFEST_VER_RE = re.compile(r'"version_number"\s*:\s*"([^"]*)"')


def read_manifest_version(manifest_path: Path) -> str | None:
    if not manifest_path.is_file():
        return None
    text = manifest_path.read_text(encoding=_READ_ENCODING)
    m = MANIFEST_VER_RE.search(text)
    return m.group(1) if m else None


def read_plugin_versions(plugin_path: Path) -> list[str]:
    text = plugin_path.read_text(encoding=_READ_ENCODING)
    return PLUGIN_VER_RE.findall(text)


def load_matrix_plugin_files() -> dict[str, tuple[str, str]]:
    matrix = json.loads(MOD_MATRIX_PATH.read_text(encoding=_READ_ENCODING))
    out: dict[str, tuple[str, str]] = {}
    for row in matrix:
        json_key = row.get("jsonKey")
        mod_dir = row.get("modDir")
        plugin_file = row.get("pluginFile")
        if not json_key or not mod_dir or not plugin_file:
            continue
        out[str(json_key)] = (str(mod_dir), str(plugin_file))
    return out


def main() -> int:
    data = json.loads(VERSIONS_PATH.read_text(encoding=_READ_ENCODING))
    mod_plugin_files = load_matrix_plugin_files()
    errors: list[str] = []

    for key, entry in data.items():
        if key not in mod_plugin_files:
            errors.append(f"Unknown mod key in versions.json (add to scripts/mod-matrix.json): {key}")
            continue

        want = entry.get("version")
        if not want:
            errors.append(f"{key}: missing version in versions.json")
            continue

        mod_dir, plugin_file = mod_plugin_files[key]
        base = REPO / mod_dir
        plugin_path = base / plugin_file
        manifest_path = base / "thunderstore" / "manifest.json"

        if not plugin_path.is_file():
            errors.append(f"{key}: missing {plugin_path.relative_to(REPO)}")
            continue

        found = read_plugin_versions(plugin_path)
        if not found:
            errors.append(f"{key}: no PLUGIN_VERSION in {plugin_path.relative_to(REPO)}")
            continue

        uniq = set(found)
        if len(uniq) != 1:
            errors.append(
                f"{key}: PLUGIN_VERSION lines disagree in {plugin_file}: {uniq}"
            )
            continue

        if found[0] != want:
            errors.append(
                f"{key}: PLUGIN_VERSION is {found[0]!r} but versions.json says {want!r} "
                f"— run: .\\scripts\\pre-push-build.ps1 -Mod <modkey> (sync), or -Mod <modkey> -Bump patch|minor|major"
            )

        mv = read_manifest_version(manifest_path)
        if manifest_path.is_file():
            if mv is None:
                errors.append(f"{key}: could not parse version_number in thunderstore/manifest.json")
            elif mv != want:
                errors.append(
                    f"{key}: manifest version_number is {mv!r} but versions.json says {want!r}"
                )

    if errors:
        print("Version consistency check failed:\n", file=sys.stderr)
        for e in errors:
            print(f"  - {e}", file=sys.stderr)
        return 1

    print(f"OK: {len(data)} mod(s) match docs/versions.json, PLUGIN_VERSION, and manifest.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
