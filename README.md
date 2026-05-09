# Sun Haven Mods

BepInEx plugins for [Sun Haven](https://store.steampowered.com/app/1432860/Sun_Haven/) — vault, QoL, integrations, and docs. **Published versions and download links** live in [`docs/versions.json`](docs/versions.json) (also drives the [mod hub](https://azraelgodking.github.io/SunhavenMod/)).

## Mods (versions match `docs/versions.json`)

| Mod | Folder | Version |
|-----|--------|---------|
| Senpai's Chest | [`SenpaisChest/`](SenpaisChest/) | 2.7.0 |
| Sun Haven Todo | [`SunhavenTodo/`](SunhavenTodo/) | 1.4.3 |
| Sun Haven Museum Utility Tracker (S.M.U.T.) | [`SunHavenMuseumUtilityTracker/`](SunHavenMuseumUtilityTracker/) | 2.4.2 |
| The Vault | [`TheVault/`](TheVault/) | 3.3.2 |
| Haven's Birthright | [`HavensBirthright/`](HavensBirthright/) | 2.2.2 |
| Haven's Almanac | [`HavensAlmanac/`](HavensAlmanac/) | 1.4.2 |
| A Squirrel's Birthday Reminder | [`BirthdayReminder/`](BirthdayReminder/) | 1.4.2 |
| Haven Dev Tools | [`HavenDevTools/`](HavenDevTools/) | 1.2.2 |
| Trinket Fortune | [`TrinketFortune/`](TrinketFortune/) | 1.2.2 |
| Faster Races | [`FasterRaces/`](FasterRaces/) | 1.4.2 |
| Crop Optimizer | [`CropOptimizer/`](CropOptimizer/) | 1.4.3 |
| Haven's Respec | [`HavensRespec/`](HavensRespec/) | 1.3.2 |

Per-mod READMEs and Thunderstore packages describe features, hotkeys, and config in detail.

## Installation

1. Install [BepInEx 5.x](https://docs.bepinex.dev/articles/user_guide/installation/index.html) for Sun Haven.
2. Download from [Thunderstore](https://thunderstore.io/c/sun-haven/) / [Nexus](https://www.nexusmods.com/sunhaven) / [GitHub Releases](https://github.com/AzraelGodKing/SunhavenMod/releases), or build from this repo.
3. Copy the mod DLL (and any packaged files) into `Sun Haven/BepInEx/plugins/` (subfolders allowed).

## Build from source

Requires a local Sun Haven install (or CI paths) so game assemblies resolve. Optional env: `SUNHAVEN_PATH` overrides `Directory.Build.props`.

```bash
dotnet build <ModDir>/<ModName>.csproj
```

Artifacts land under `bin/Debug/net48/` (or Release). Local builds may copy into your game’s BepInEx plugins folder when configured.

Multi-mod solution (subset of mods): [`HavensBirthright/HavensBirthright.sln`](HavensBirthright/HavensBirthright.sln).

## Repository layout

| Path | Purpose |
|------|---------|
| `<ModName>/` | One folder per mod + `thunderstore/` packaging |
| [`SharedUtilities/`](SharedUtilities/) | Shared source linked into mods (VersionChecker, IconCache, etc.) |
| [`TheVault.Abstractions/`](TheVault.Abstractions/) | Compile-time API surface for soft-dependent mods |
| [`scripts/`](scripts/) | Version checks, mod matrix, stats fetch, pre-push helpers |
| [`docs/`](docs/) | GitHub Pages site, `versions.json`, mod HTML guides |
| [`.github/workflows/`](.github/workflows/) | CI, release, stats, site mirror |

The `builds/` directory is **ephemeral** (CI/local staging); do not commit it — see [`builds/README.md`](builds/README.md).

## Changelogs and contributing

- **Players:** [`CHANGELOG.md`](CHANGELOG.md)
- **Maintainers:** [`MAINTAINER_CHANGELOG.md`](MAINTAINER_CHANGELOG.md) (CI, scripts, infra)

[`CONTRIBUTING.md`](CONTRIBUTING.md) · [`SECURITY.md`](SECURITY.md) · [`LICENSE`](LICENSE)

## Links

- [Documentation hub](https://azraelgodking.github.io/SunhavenMod/)
- [Version & release process](docs/VERSION_AND_RELEASE.md)
- [Discord](https://discord.gg/Vwh2y7qMXv)