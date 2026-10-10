#!/usr/bin/env python3
"""Exit 0 when Thunderstore already has this namespace/name/version.

Used after an upload that returned
"Package of the same namespace, name and version already exists".
Any other upload failure still fails the job.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from release_version_gate import already_published, thunderstore_latest


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--namespace", required=True)
    parser.add_argument("--name", required=True)
    parser.add_argument("--version", required=True)
    args = parser.parse_args()
    remote = thunderstore_latest("", args.namespace, args.name)
    if already_published(args.version, remote):
        print(
            f"Thunderstore already has {args.namespace}-{args.name} "
            f"{args.version} (latest {remote}). Skipping."
        )
        return 0
    print(
        f"::error::Thunderstore upload failed and {args.namespace}-{args.name} "
        f"{args.version} is not published (latest {remote or 'unknown'}).",
        file=sys.stderr,
    )
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
