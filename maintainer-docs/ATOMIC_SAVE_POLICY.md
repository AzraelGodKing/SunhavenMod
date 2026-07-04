# Atomic save / tmp-file policy (mods)

Different mods use slightly different **failure semantics** when persisting JSON/binary saves. All paths aim to avoid silently committing corrupt primary files.

| Mod / subsystem | Temp file | On failure |
|-------------------|-----------|------------|
| **The Vault** (`VaultSaveSystem.Save`) | Writes to `.tmp`, compares paths, replace → backup → live. | Keeps `.tmp` if replace fails so operators can recover payload manually (see `VaultSaveSystem.cs`). |
| **Senpai's Chest** (`SmartChestSaveSystem`) | Temp + move to main via `CharacterSaveStore`. | `finally` removes `.tmp` after move attempt; live file may be previous gen or `.bak` from prior step. |
| **Sunhaven Todo** (`TodoSaveSystem`) | Same shared helper. | Same as Senpai's Chest: `finally` deletes `.tmp`. |
| **Gifting Assistant**, **S.M.U.T.** | Same shared `CharacterSaveStore`. | Same `.bak` rotate + `.tmp` cleanup contract. |

Shared helper: `SharedUtilities/CharacterSaveStore.cs`. **The Vault** (encrypted `.backup` suffix) and **Birthday Reminder** favorites (line-based, no `.bak` yet) still use their own paths until migrated.

Do not unify implementations blindly — each subsystem predates the others; changing discard semantics can strand recoverable crash artifacts. When touching save code, preserve these contracts or update **player-facing** changelog notes.
