#!/usr/bin/env python3
"""Generate reusable-release-mod.yml from build-release-publish.yml release steps."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MAIN = ROOT / ".github/workflows/build-release-publish.yml"
OUT = ROOT / ".github/workflows/reusable-release-mod.yml"

MODS = [
    "senpaischest",
    "havensbirthright",
    "sunhavenmuseumutilitytracker",
    "squirrelsbirthdayreminder",
    "sunhaventodo",
    "thevault",
    "havendevtools",
    "havensalmanac",
    "fasterraces",
    "trinketfortune",
    "cropoptimizer",
    "havensrespec",
]

HEADER = """name: Reusable — release one mod

on:
  workflow_call:
    inputs:
      mod:
        type: string
        required: true
      ref:
        type: string
        required: true
      dry_run:
        type: string
        default: "false"
      publish_thunderstore:
        type: string
        default: "false"
      publish_nexus:
        type: string
        default: "false"
      create_github_release:
        type: string
        default: "false"
      release_anyway:
        type: string
        default: "false"
      test_discord:
        type: string
        default: "false"
      publish_discord_only:
        type: string
        default: "false"
      ignore_discord_notify:
        type: string
        default: "false"

env:
  THUNDERSTORE_NAMESPACE: "AzraelGodKing"
  THUNDERSTORE_COMMUNITY: "sun-haven"
  THUNDERSTORE_CATEGORIES: "mods"
  THUNDERSTORE_DEPS: "BepInEx-BepInExPack@5.4.2100"

jobs:
  release-mod:
    runs-on: ubuntu-latest
    permissions:
      contents: write
    steps:
"""

PICK_BLOCK_START = "      - name: Select mod for this matrix row"
PICK_BLOCK_END = '            echo "selected=true" >> "$GITHUB_OUTPUT"\n          fi\n'


def transform_steps(raw: str) -> str:
    text = raw
    text = text.replace("${{ needs.build-gate.outputs.ref }}", "${{ inputs.ref }}")
    text = text.replace("${{ matrix.mod }}", "${{ inputs.mod }}")
    text = text.replace("github.event.inputs.", "inputs.")
    text = text.replace("steps.pick.outputs.selected == 'true' && ", "")
    text = text.replace("steps.pick.outputs.selected == 'true' &&\n          ", "")
    text = text.replace(
        "steps.pick.outputs.selected == 'true' &&\n          success()",
        "success()",
    )
    text = text.replace(
        "always() &&\n          steps.pick.outputs.selected == 'true' &&",
        "always() &&",
    )
    text = text.replace("if: steps.pick.outputs.selected == 'true' && always()", "if: always()")
    text = text.replace("if: steps.pick.outputs.selected == 'true'", "if: true")
    return text


def extract_steps(lines: list[str]) -> str:
    start = next(
        i
        for i, line in enumerate(lines)
        if line.startswith("    steps:") and i > 300
    )
    end = next(
        i for i, line in enumerate(lines) if line.strip().startswith("release_aggregate_report:")
    )
    chunk = lines[start + 1 : end]
    pick_start = next(i for i, line in enumerate(chunk) if PICK_BLOCK_START in line)
    pick_end = next(
        i
        for i, line in enumerate(chunk[pick_start:], pick_start)
        if line.strip() == "fi" and i > pick_start + 3
    )
    chunk = chunk[:pick_start] + chunk[pick_end + 1 :]
    return transform_steps("".join(chunk))


def release_job_yaml(mod: str) -> str:
    return f"""  release-{mod}:
    name: release ({mod})
    if: >-
      github.event_name == 'workflow_dispatch' &&
      github.event.inputs.build_only != 'true' &&
      needs.build-gate.result == 'success' &&
      (github.event.inputs.mod == 'all' || github.event.inputs.mod == '{mod}')
    needs: build-gate
    permissions:
      contents: write
    uses: ./.github/workflows/reusable-release-mod.yml
    with:
      mod: {mod}
      ref: ${{{{ needs.build-gate.outputs.ref }}}}
      dry_run: ${{{{ github.event.inputs.dry_run }}}}
      publish_thunderstore: ${{{{ github.event.inputs.publish_thunderstore }}}}
      publish_nexus: ${{{{ github.event.inputs.publish_nexus }}}}
      create_github_release: ${{{{ github.event.inputs.create_github_release }}}}
      release_anyway: ${{{{ github.event.inputs.release_anyway }}}}
      test_discord: ${{{{ github.event.inputs.test_discord }}}}
      publish_discord_only: ${{{{ github.event.inputs.publish_discord_only }}}}
      ignore_discord_notify: ${{{{ github.event.inputs.ignore_discord_notify }}}}
    secrets: inherit

"""


def patch_main_workflow(lines: list[str]) -> list[str]:
    release_start = next(i for i, line in enumerate(lines) if line.strip() == "release:")
    agg_start = next(i for i, line in enumerate(lines) if line.strip().startswith("release_aggregate_report:"))
    release_jobs = "".join(release_job_yaml(m) for m in MODS)
    block = (
        "  # ---------------------------------------------------------------------------\n"
        "  # Release — one reusable workflow job per mod (avoids matrix+needs(build) skip bug).\n"
        "  # ---------------------------------------------------------------------------\n"
        + release_jobs
    )
    new_lines = lines[:release_start] + [block] + lines[agg_start:]

    # Patch aggregate needs
    for i, line in enumerate(new_lines):
        if line.strip().startswith("needs: [build-gate, release]"):
            needs = ["build-gate"] + [f"release-{m}" for m in MODS]
            indent = line[: line.index("needs")]
            replacement = [indent + "needs:\n"]
            replacement.extend(indent + "  - " + n + "\n" for n in needs)
            new_lines[i : i + 1] = replacement
            break

    for i, line in enumerate(new_lines):
        if "**Release job result:** \\`${{ needs.release.result }}\\`" in line:
            new_lines[i] = line.replace(
                "needs.release.result",
                "needs.build-gate.result",
            )
            break

    # Remove old skip condition on needs.release.result
    for i, line in enumerate(new_lines):
        if "needs.release.result != 'skipped'" in line:
            new_lines.pop(i)
            break

    return new_lines


def main() -> None:
    lines = MAIN.read_text(encoding="utf-8").splitlines(keepends=True)
    steps = extract_steps(lines)
    OUT.write_text(HEADER + steps, encoding="utf-8")
    patched = patch_main_workflow(lines)
    MAIN.write_text("".join(patched), encoding="utf-8")
    print(f"Wrote {OUT}")
    print(f"Patched {MAIN}")


if __name__ == "__main__":
    main()
