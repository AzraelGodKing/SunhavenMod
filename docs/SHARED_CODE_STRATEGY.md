# Shared code strategy (`SharedUtilities/`)

## Current approach

Each mod **links** sources from [`SharedUtilities/`](../SharedUtilities/) via `<Compile Include="..\SharedUtilities\*.cs" Link="Shared\...">` in its `.csproj`. Every plugin DLL therefore embeds its **own** copy of types such as `SunhavenMods.Shared.VersionChecker` — they are **different CLR types** across assemblies.

**Pros:** Single Thunderstore/Nexus package per mod; no extra shared DLL to ship/version.

**Cons:** Cross-mod features that rely on a single type identity (e.g. aggregating static state) require reflection across assemblies — Haven's Almanac **Mod Health** does this on purpose.

## Alternative (not default here)

Ship one **`SunhavenMods.Shared.dll`** next to plugins and reference it from every mod. **Pros:** one type identity. **Cons:** deployment/version skew if mods update shared DLL independently.

Evaluation: stay with linked sources unless we introduce a formal shared package with semver and a single install location under `BepInEx/plugins/`.
