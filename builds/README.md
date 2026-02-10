# Pre-Built DLLs for Release

Place your compiled DLLs here before running the GitHub Actions workflow.

## Structure

```
builds/
├── HavensBirthright/
│   └── HavensBirthright.dll
├── SunHavenMuseumUtilityTracker/
│   └── SunHavenMuseumUtilityTracker.dll
├── BirthdayReminder/
│   └── BirthdayReminder.dll
├── SunhavenTodo/
│   └── SunhavenTodo.dll
├── TheVault/
│   └── TheVault.dll
└── HavenDevTools/
    └── HavenDevTools.dll
```

## Workflow

1. Build your mod locally (Visual Studio / Rider)
2. Copy the DLL from `bin/Release/` to the appropriate folder here
3. Commit and push: `git add builds/ && git commit -m "Update build" && git push`
4. Go to GitHub Actions → "Release & Publish" → Run workflow
5. Select your mod and whether to publish to Thunderstore
