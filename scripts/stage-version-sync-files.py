#!/usr/bin/env python3
"""
Stage only files that pre-push-build.ps1 -SyncAll may touch.
Used by .github/workflows/sync-mod-versions.yml so the bot commit never picks up unrelated edits.
"""
from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
MATRIX = REPO / "scripts" / "mod-matrix.json"


def main() -> int:
    data = json.loads(MATRIX.read_text(encoding="utf-8"))
    paths: set[Path] = {
        REPO / "README.md",
        REPO / "docs" / "versions.json",
        REPO / "docs" / "index.html",
        REPO / "HavenDevTools" / "UI" / "DebugWindow.cs",
    }
    for row in data:
        mod = row["modDir"]
        plugin = row.get("pluginFile") or "Plugin.cs"
        paths.add(REPO / mod / plugin)
        paths.add(REPO / mod / "thunderstore" / "manifest.json")
        paths.add(REPO / mod / f"NexusMods-{mod}-BBCode.txt")
        paths.add(REPO / mod / "thunderstore" / "README.md")
        paths.add(REPO / row["csproj"])
        for ex in row.get("extraCsprojPaths") or []:
            paths.add(REPO / ex)
        dhp = row.get("docsPagePath")
        if dhp:
            paths.add(REPO / "docs" / dhp)
    existing = [p for p in sorted(paths, key=lambda x: str(x)) if p.is_file()]
    if not existing:
        print("stage-version-sync-files: no known paths exist — skipping git add", file=sys.stderr)
        return 0
    subprocess.run(["git", "add", "--"] + [str(p.relative_to(REPO)).replace("\\", "/") for p in existing], cwd=REPO, check=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
