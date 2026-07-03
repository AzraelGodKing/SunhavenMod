# Community mods (local only)

Mods in this folder are **not** part of the official release pipeline. They are **not** listed in [`scripts/matrix/mod-matrix.json`](../scripts/matrix/mod-matrix.json), excluded from CI version checks, and their project folders are **gitignored** so source stays on your machine.

## Ultra Polygamy

Sun Haven 3.1–compatible port of the community **Ultra Polygamy** mod (`vurawnica.sunhaven.polygamy` v0.0.5). Same features and config as the original; only Harmony/decompiler fixes for 3.1.

**Build (from repo root):**

```powershell
dotnet build CommunityMods/UltraPolygamy/UltraPolygamy.csproj -c Release
```

Output: `CommunityMods/UltraPolygamy/bin/Release/net48/UltraPolygamy.dll`

With default `Directory.Build.props`, a successful local build also copies the DLL to your Sun Haven `BepInEx/plugins/` folder when `SunhavenCopyToBepInExPlugins` is true.

**Config:** `BepInEx/config/UltraPolygamy.cfg`

To track your own fork of the source, remove `CommunityMods/UltraPolygamy/` from `.gitignore` or force-add with `git add -f`.
