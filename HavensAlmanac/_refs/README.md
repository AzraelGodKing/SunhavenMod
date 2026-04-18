# `_refs/` — decompiled game code (reference only)

Files in this folder are **not compiled** into `HavensAlmanac.dll`. The csproj
explicitly excludes them with `<Compile Remove="_refs\**\*.cs" />` and includes
them as `<None>` so they still travel with the repo for grep-ability in your
editor but never land in the shipped plugin.

## What's in here

- `Inventory_decompiled.cs` — dnSpy dump of `Wish.Inventory` from the game's
  `Assembly-CSharp.dll`. Used as a reference when we need to know the shape of
  the inventory/item APIs that Haven's Almanac has to touch via reflection
  (see `Integration/` providers). Not all field/method names survive
  decompilation cleanly, so treat this as a hint, not ground truth.

## Rules

- Never `using` or `namespace`-alias anything from `_refs/` in real code — the
  types live in `Assembly-CSharp.dll`, not in our DLL.
- If you add another decompile here, keep it isolated to this folder and add
  a one-line note above about where it came from.
- These files are re-derived from the game at any time, so do not rely on them
  as a canonical API contract.
