# Justice For Harold

A small Sun Haven mod that adds a simple quest for the NPC **Harold**, who has lost his fishing nets.

## What it does

- When you talk to **Harold** before the quest is done, he asks if you can spare a fishing net.
- If you have at least one **Fishing Net** (item ID 10502), you can choose **"Here, take this."** to give him one.
- After giving a net: the quest is marked complete, Harold gains relationship with the player, and he thanks you.
- If you don't have a net (or choose not to give one), you can decline; Harold's override dialogue won't show again until you have a net (or you can say "Maybe another time." / "I don't have any.").

## Requirements

- Sun Haven (with BepInEx 5.x)
- No other mods required

## Installation

1. Install [BepInEx 5](https://docs.bepinex.dev/) for Sun Haven if you haven't already.
2. Build the project (or use a pre-built `JusticeForHarold.dll`) and place `JusticeForHarold.dll` in `BepInEx/plugins/JusticeForHarold/`.
3. Start the game and talk to Harold.

## Build

Open the solution or project in the repo, set `SUNHAVEN_PATH` (or use the default path in `Directory.Build.props`) to your Sun Haven install, then build. The build target copies the DLL to `BepInEx/plugins/JusticeForHarold/` and to `builds/JusticeForHarold/`.

## Technical notes

- Progress is stored under the character flag **JusticeForHaroldComplete**.
- Relationship reward is **+2** hearts (hardcoded).
- The mod uses a Harmony prefix on `NPCAI.Interact(int)` and injects dialogue via `OverrideDialogue` only for Harold when the quest is not complete.
