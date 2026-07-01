#!/usr/bin/env python3
"""Set changelog field(s) in docs/versions.json before a CI version bump."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
VERSIONS_PATH = REPO / "docs" / "versions.json"
MATRIX_PATH = REPO / "scripts" / "matrix" / "mod-matrix.json"


def json_keys_for_mod(mod_key: str | None, all_mods: bool) -> list[str]:
    rows = json.loads(MATRIX_PATH.read_text(encoding="utf-8"))
    if all_mods:
        return [row["jsonKey"] for row in rows if row.get("jsonKey")]
    for row in rows:
        if row.get("modKey") == mod_key:
            jk = row.get("jsonKey")
            if not jk:
                raise SystemExit(f"mod-matrix.json row {mod_key!r} missing jsonKey")
            return [jk]
    raise SystemExit(f"Unknown mod key {mod_key!r} (see scripts/matrix/mod-matrix.json)")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--mod", help="modKey from mod-matrix.json (omit with --all)")
    parser.add_argument("--all", action="store_true", help="Update every mod in mod-matrix.json")
    parser.add_argument("--text", required=True, help="Changelog body stored in versions.json")
    args = parser.parse_args()

    if args.all and args.mod:
        raise SystemExit("Use --all or --mod, not both")
    if not args.all and not args.mod:
        raise SystemExit("Specify --mod <modKey> or --all")

    keys = json_keys_for_mod(args.mod, args.all)
    data = json.loads(VERSIONS_PATH.read_text(encoding="utf-8"))

    for key in keys:
        if key not in data:
            raise SystemExit(f"{key!r} missing from docs/versions.json")
        data[key]["changelog"] = args.text

    VERSIONS_PATH.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Updated changelog for {len(keys)} mod(s) in docs/versions.json")


if __name__ == "__main__":
    main()
