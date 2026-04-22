# Changelog

## 2026-04-22
### CI
- Guarded release tag context access in `.github/workflows/build-release-publish.yml` to avoid workflow validation warnings about `github.event.release`.
- Upgraded workflow artifact actions to Node 24-ready majors (`actions/upload-artifact@v6`, `actions/download-artifact@v7`) in active and deprecated release workflows.

### Scripts
- Renamed a local regex capture variable in `scripts/verify-version-consistency.ps1` to avoid assigning to PowerShell automatic variable names flagged by `PSAvoidAssignmentToAutomaticVariable`.
- Simplified brace-heavy error-string construction in `scripts/verify-version-consistency.ps1` to avoid parser/linter false positives about missing closing `}`.

### Docs
- Updated all mod Nexus BBCode assets to the current formatted template, synchronized versioned headers, and normalized section/link wording for consistent Nexus copy.
- Expanded `docs/NexusMods-BBCode-Index.txt` to include `CropOptimizer` and `HavensRespec`.

### Web
- Added `wrangler.toml` and `cloudflare-site/` so Cloudflare Pages work targets a separate directory instead of `docs/`, preserving existing GitHub Pages content.
- Added npm scripts for Cloudflare Pages (`cf:pages:dev`, `cf:pages:deploy`) so deployments use `wrangler pages deploy` instead of Worker-mode `wrangler deploy`.
- Replaced the Cloudflare placeholder page with a structured starter homepage in `cloudflare-site/index.html` and added `cloudflare-site/styles.css` for a reusable base style system.
- Upgraded `cloudflare-site` landing page to a more polished marketing-style hero with CTAs, stat highlights, feature framing, and improved visual styling while keeping the rollout static/simple.
- Added `.github/workflows/sync-cloudflare-site-mirror.yml` to mirror `cloudflare-site/` (plus `wrangler.toml` / `package*.json` when present) from `SunhavenMod` to `AzraelGodKing/azrael-sunhaven-website` on pushes to `main` (uses `AZRAEL_WEBSITE_SYNC_TOKEN` when set, otherwise falls back to `ADMIN_PUSH_TOKEN`).

### Docs
- Updated branch guidance from `master` to `main` in release/version docs and repo cleanup conventions, and narrowed `.github/workflows/sync-mod-versions.yml` push trigger to `main`.

## 2026-04-21
### Performance
- Fixed UI texture leaks by disposing stale textures before recreating them in chest and todo interfaces.
- Removed recurring runtime allocations in UI and item patch paths by caching styles, scene/season checks, and helper parsing flows.
- Added bounded eviction to `SharedUtilities/IconCache.cs` to prevent unbounded texture cache growth.

### CI
- Promoted the self-hosted release workflow to `.github/workflows/build-release-publish.yml`.
- Archived the previous release workflow to `.github/workflows/deprecated/build-release-publish.yml` for rollback reference.
