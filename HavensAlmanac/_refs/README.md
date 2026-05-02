# `_refs/` — local reference notes

**Do not commit decompiled game source.** Sun Haven’s EULA terms restrict redistribution of game binaries and derived source; maintainers should generate dnSpy/ILSpy dumps **locally only** (see `.gitignore` for `*_decompiled.cs` patterns).

## Using this folder

- Keep short **hand-written notes** here if useful (API shapes you confirmed via reflection), or leave the folder empty.
- Real code must continue to use reflection against `Assembly-CSharp.dll` at runtime — see `Integration/` providers.

## Rules

- Never `using` or namespace-alias anything from committed `_refs/` stubs in shipping code.
- If you need a decompile for grep in your editor, place `Inventory_decompiled.cs` (or similar) **locally**; it will be ignored by git when matching ignore patterns.
