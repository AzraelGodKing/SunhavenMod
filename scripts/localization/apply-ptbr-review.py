#!/usr/bin/env python3
"""Apply pt-BR translations from review markdown; update only pt-BR lines in strings.json."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

MOD_NAME_TO_DIR = {
    "Birthday Reminder": "BirthdayReminder",
    "Crop Optimizer": "CropOptimizer",
    "Haven Dev Tools": "HavenDevTools",
    "Haven's Almanac": "HavensAlmanac",
    "Haven's Respec": "HavensRespec",
    "S.M.U.T.": "SunHavenMuseumUtilityTracker",
    "Senpai's Chest": "SenpaisChest",
    "Sun Haven Todo": "SunhavenTodo",
    "The Vault": "TheVault",
}


def unescape_cell(text: str) -> str:
    text = text.strip()
    text = text.replace(r"\|", "|")
    text = text.replace("<br>", "\n")
    return text


def parse_review(path: Path) -> dict[str, dict[str, str]]:
    content = path.read_text(encoding="utf-8")
    updates: dict[str, dict[str, str]] = {}
    current_mod: str | None = None

    section_re = re.compile(r"^## (.+?) \(\d+ strings\)\s*$")
    row_re = re.compile(r"^\|\s*`([^`]+)`\s*\|\s*(.+?)\s*\|\s*(.+?)\s*\|$")

    for line in content.splitlines():
        section = section_re.match(line)
        if section:
            name = section.group(1).strip()
            current_mod = MOD_NAME_TO_DIR.get(name)
            if current_mod and current_mod not in updates:
                updates[current_mod] = {}
            elif not current_mod:
                print(f"warning: unknown mod section {name!r}", file=sys.stderr)
            continue

        if not current_mod:
            continue
        if not line.startswith("|") or line.startswith("| ---"):
            continue
        if line.startswith("| Key |"):
            continue

        row = row_re.match(line)
        if not row:
            continue
        key, _, pt_br = row.groups()
        updates[current_mod][key.strip()] = unescape_cell(pt_br)

    return updates


def json_escape(value: str) -> str:
    return json.dumps(value, ensure_ascii=False)[1:-1]


def apply_to_file(strings_path: Path, key_updates: dict[str, str]) -> tuple[int, int]:
    lines = strings_path.read_text(encoding="utf-8").splitlines(keepends=True)
    current_key: str | None = None
    updated = 0
    missing = 0

  # Track which keys we found in file
    found_keys: set[str] = set()

    for i, line in enumerate(lines):
        key_match = re.match(r'^\s*"([^"]+)":\s*\{', line)
        if key_match:
            current_key = key_match.group(1)
            continue

        if current_key and '"pt-BR"' in line and current_key in key_updates:
            indent = re.match(r"^(\s*)", line).group(1)
            escaped = json_escape(key_updates[current_key])
            lines[i] = f'{indent}"pt-BR":  "{escaped}",\n'
            updated += 1
            found_keys.add(current_key)
            continue

        if current_key and re.match(r"^\s*\},?\s*$", line):
            current_key = None

    for key in key_updates:
        if key not in found_keys:
            print(f"warning: [{strings_path.parent.parent.name}] key not found: {key}", file=sys.stderr)
            missing += 1

    if updated:
        strings_path.write_text("".join(lines), encoding="utf-8", newline="\n")

    return updated, missing


def main() -> int:
    if len(sys.argv) < 3:
        print("usage: apply-ptbr-review.py <review.md> <repo-root>", file=sys.stderr)
        return 1

    review_path = Path(sys.argv[1])
    repo_root = Path(sys.argv[2])
    updates = parse_review(review_path)

    total_updated = 0
    total_missing = 0

    for mod_dir, key_updates in sorted(updates.items()):
        strings_path = repo_root / mod_dir / "Localization" / "strings.json"
        if not strings_path.is_file():
            print(f"warning: missing {strings_path}", file=sys.stderr)
            continue
        count, missing = apply_to_file(strings_path, key_updates)
        total_updated += count
        total_missing += missing
        if count:
            print(f"[{mod_dir}] Updated {count} pt-BR strings")

    print(f"Done. Updated {total_updated} strings; {total_missing} keys not found.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
