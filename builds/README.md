# Local / CI staging (`builds/`)

This directory is **ignored by git** (see root `.gitignore`). Release workflows and local scripts write compiled DLLs here for packaging **during a single run** — nothing here should be committed.

## Typical layout (ephemeral)

Workflows recreate `builds/<ModDir>/<dll>.dll` when building; artifacts are uploaded from this tree.

## Developing locally

After `dotnet build`, optional post-build steps may copy outputs into `builds/<YourMod>/` for Thunderstore zip scripting or runner packaging. **Do not** `git add builds/`; CI builds from source instead of relying on checked-in binaries.

See [`maintainer-docs/VERSION_AND_RELEASE.md`](../maintainer-docs/VERSION_AND_RELEASE.md) for the release pipeline.
