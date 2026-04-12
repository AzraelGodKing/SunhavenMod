# Pre-Built DLLs for Release

Place your compiled DLLs here before running the GitHub Actions workflow.

## Structure

```
builds/
├── BirthdayReminder/
│   └── BirthdayReminder.dll
├── FasterRaces/
│   └── FasterRaces.dll
├── HavenDevTools/
│   └── HavenDevTools.dll
├── HavensAlmanac/
│   └── HavensAlmanac.dll
├── HavensBirthright/
│   └── HavensBirthright.dll
├── JusticeForHarold/
│   └── JusticeForHarold.dll
├── SenpaisChest/
│   └── SenpaisChest.dll
├── SunHavenMuseumUtilityTracker/
│   └── SunHavenMuseumUtilityTracker.dll
├── SunhavenTodo/
│   └── SunhavenTodo.dll
├── TheVault/
│   ├── TheVault.dll
│   └── TheVault.Abstractions.dll
└── TrinketFortune/
    └── TrinketFortune.dll
```

## Workflow

1. Build your mod locally (Visual Studio / Rider)
2. Copy release DLLs from `bin/Release/` to the appropriate folder here
3. For The Vault releases, include both `TheVault.dll` and `TheVault.Abstractions.dll`
4. Commit and push: `git add builds/ && git commit -m "Update build" && git push`
5. Go to GitHub Actions → "Release & Publish" → Run workflow
6. Select your mod and whether to publish to Thunderstore
