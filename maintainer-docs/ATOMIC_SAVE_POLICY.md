# Atomic save / tmp-file policy (mods)

Different mods use slightly different **failure semantics** when persisting JSON/binary saves. All paths aim to avoid silently committing corrupt primary files.

| Mod / subsystem | Temp file | On failure |
|-------------------|-----------|------------|
| **The Vault** (`VaultSaveSystem.Save`) | `.tmp` via `CharacterSaveStore.WriteAtomicBytes` (`.backup` rotate). | Keeps `.tmp` if replace fails (`deleteTempInFinally: false`) so operators can recover payload manually. |
| **Senpai's Chest** (`SmartChestSaveSystem`) | Temp + move to main via `CharacterSaveStore`. | `finally` removes `.tmp` after move attempt; live file may be previous gen or `.bak` from prior step. |
| **Sunhaven Todo** (`TodoSaveSystem`) | Same shared helper. | Same as Senpai's Chest: `finally` deletes `.tmp`. |
| **Gifting Assistant**, **S.M.U.T.** | Same shared `CharacterSaveStore`. | Same `.bak` rotate + `.tmp` cleanup contract. |
| **Birthday Reminder** favorites | Same shared helper (line-based `.txt`). | Same `.bak` rotate + `.tmp` cleanup contract. |

Shared helper: `SharedUtilities/CharacterSaveStore.cs` (sanitize path, atomic text/bytes write, backup fallback load). JSON mods use `.bak`; The Vault uses `.backup`.

Do not unify implementations blindly — each subsystem predates the others; changing discard semantics can strand recoverable crash artifacts. When touching save code, preserve these contracts or update **player-facing** changelog notes.
