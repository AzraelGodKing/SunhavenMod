# Versioning and release (single pipeline)

This repo uses **one release version per mod** in [`docs/versions.json`](../docs/versions.json). GitHub Actions, Thunderstore, and Nexus all read that file. The compiled DLL must report the **same** version (`PLUGIN_VERSION` / BepInEx metadata), or players and update checks will disagree with the store.

## Order of operations (required)

1. **Edit release metadata** for the mod(s) you will ship:
   - In `docs/versions.json`: bump **`version`**, update **`changelog`** (and any `vault_save_breaking` / banner fields for The Vault when needed).

2. **Propagate the version everywhere** the maintainer script knows about (plugin source, Thunderstore manifest, readmes, etc.):
   - From the repo root on **Windows** (PowerShell), either:
     - **Bump and sync:**  
       `.\scripts\pre-push-build.ps1 -Mod <modkey> -Bump patch`  
       (use `minor` or `major` when appropriate), **or**
     - **Sync only** (version already edited in `versions.json`):  
       `.\scripts\pre-push-build.ps1 -Mod <modkey>`  

   Mod keys match the workflow picker: `senpaischest`, `havensbirthright`, `sunhavenmuseumutilitytracker`, `squirrelsbirthdayreminder`, `sunhaventodo`, `thevault`, `havendevtools`, `havensalmanac`, `fasterraces`, `trinketfortune`, `cropoptimizer`, etc. Use `-All` for every mod (see script help).

3. **Commit and push** to the branch your workflows use (usually `main`).

4. **CI check:** the **Release & Publish** and **Test — Self-hosted Sunhaven runner** workflows run [`scripts/verify-version-consistency.py`](../scripts/verify-version-consistency.py) in the setup job. If `versions.json`, `PLUGIN_VERSION`, and `thunderstore/manifest.json` disagree, the workflow fails before building.

5. **Publish:** run **Release & Publish** on GitHub Actions (manual dispatch). Toggle GitHub Release / Thunderstore / Nexus as needed. The workflow builds on your self-hosted runner, then packages and uploads.

## Why version must come before the release build

- The **duplicate-version guard** compares `docs/versions.json` to the **last GitHub release tag** for that mod. Shipping without bumping would skip or confuse releases.
- The **DLL** must be built from sources that already contain the new `PLUGIN_VERSION`, or the artifact version and the published metadata will not match.

## Local checks

If you only need to validate consistency (for example before pushing), use either verifier:

PowerShell (no Python required):

```powershell
.\scripts\verify-version-consistency.ps1
```

Python:

```bash
python3 scripts/verify-version-consistency.py
```

The Python verifier requires Python 3.6+.

## Mod matrix ownership

Structural mod metadata now lives in [`scripts/mod-matrix.json`](../scripts/mod-matrix.json). This is the single source of truth for mod wiring used by scripts and workflows (mod keys, directories, plugin file path, csproj path, Thunderstore package name, docs page path, docs label aliases).

When adding a new mod:

1. Add one entry to `scripts/mod-matrix.json`.
2. Add the mod's release metadata entry in [`docs/versions.json`](../docs/versions.json).
3. Add the same docs mapping row in [`docs/mod-matrix.json`](../docs/mod-matrix.json) for hub/page version badge resolution.

Required matrix fields per row: `modKey`, `jsonKey`, `modDir`, `pluginFile`, `dllName`, `csproj`, `thunderstoreName`, `readmePath`, `indexDataName`, `docsPagePath`, `extraCsprojPaths`.

## Automatic sync on merge (optional drift repair)

The workflow [`.github/workflows/sync-mod-versions.yml`](../.github/workflows/sync-mod-versions.yml) runs on every push to `main` / `master`. It executes `.\scripts\pre-push-build.ps1 -All -SyncOnly` (no `dotnet build`), then `python3 scripts/verify-version-consistency.py`. If any tracked file was behind `docs/versions.json`, it commits and pushes with message `chore: sync mod versions from versions.json [skip ci]` so the job does not re-trigger in a loop.

**Source of truth remains `docs/versions.json`.** The workflow only copies those values into plugin sources and store metadata; it does not invent new semver bumps.

## Related files

| Piece | Role |
|--------|------|
| `docs/versions.json` | Source of truth for semver, changelog, store links |
| `scripts/pre-push-build.ps1` | Bump/sync version across plugin, manifests, docs (`-SyncOnly` for CI without game DLLs) |
| `scripts/verify-version-consistency.py` | CI + local guard: JSON vs plugin vs manifest |
| `scripts/verify-version-consistency.ps1` | PowerShell equivalent guard (no Python dependency) |
| `.github/workflows/sync-mod-versions.yml` | After merge: align tracked files with `versions.json` if needed |
| `.github/workflows/build-release-publish.yml` | Build → package → GitHub / Thunderstore / Nexus |
