# Atomic save / tmp-file policy (mods)

Different mods use slightly different **failure semantics** when persisting JSON/binary saves. All paths aim to avoid silently committing corrupt primary files.

| Mod / subsystem | Temp file | On failure |
|-------------------|-----------|------------|
| **The Vault** (`VaultSaveSystem.Save`) | Writes to `.tmp`, compares paths, replace → backup → live. | Keeps `.tmp` if replace fails so operators can recover payload manually (see `VaultSaveSystem.cs`). |
| **Senpai's Chest** (`SmartChestSaveSystem`) | Temp + move to main. | `finally` removes `.tmp` after move attempt; live file may be previous gen or `.bak` from prior step. |
| **Sunhaven Todo** (`TodoSaveSystem`) | Same general pattern. | Same as Senpai’s Chest: `finally` deletes `.tmp`. |

Do not unify implementations blindly — each subsystem predates the others; changing discard semantics can strand recoverable crash artifacts. When touching save code, preserve these contracts or update **player-facing** changelog notes.
