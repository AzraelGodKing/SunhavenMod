# A Squirrel's Birthday Reminder

Never miss NPC birthdays in Sun Haven.

## Default Behavior

- Shows birthday reminders automatically on in-game day start.
- Tracks gifted NPCs for the day.
- Supports optional SunhavenTodo integration.

## Config

File: `Sun Haven/BepInEx/config/BirthdayReminder.cfg`

## Notes

- This README is intentionally concise for repository maintenance.
- Full player-facing documentation is in `thunderstore/README.md`.

## Changelog

### Unreleased

- **Changed:** Fuzzy NPC-name fallback is stricter for short names (3-4 chars) to reduce wrong matches from Levenshtein proximity.
- **Build:** Optional `SunhavenTodo` integration now references [`../SunhavenTodo/SunhavenTodo.csproj`](../SunhavenTodo/SunhavenTodo.csproj) instead of `..\builds\SunhavenTodo\SunhavenTodo.dll` so a clean `dotnet build` succeeds.
- **Lifecycle:** Added scene/integration teardown cleanup and expected-teardown lifecycle logging on plugin destroy.
- **Debug:** Replaced silent todo reflection fallback in integration with debug logging.
