# Contributing

## Workflow

- Use feature branches; keep PRs focused.
- Run **`python scripts/version/verify-version-consistency.py`** (or `npm run sync-mod-matrix` after editing [`scripts/matrix/mod-matrix.json`](scripts/matrix/mod-matrix.json)) before pushing.
- Follow [`maintainer-docs/VERSION_AND_RELEASE.md`](maintainer-docs/VERSION_AND_RELEASE.md) for version bumps — **do not bump versions unless the maintainer requests it.**

## Shared utilities

Mods compile linked copies of [`SharedUtilities/`](SharedUtilities/) into each DLL. See [`maintainer-docs/SHARED_CODE_STRATEGY.md`](maintainer-docs/SHARED_CODE_STRATEGY.md) for trade-offs vs a shared satellite assembly.

## Changelog

Maintainers update [`CHANGELOG.md`](CHANGELOG.md) (and [`MAINTAINER_CHANGELOG.md`](MAINTAINER_CHANGELOG.md) when appropriate) when merging substantive changes.
