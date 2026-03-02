# New action: Discord release notify

The plan for the new GitHub Action (inspired by [SethCohen/github-releases-to-discord](https://github.com/SethCohen/github-releases-to-discord), with attribution) lives in the **DiscordBot** repo.

**See:** [DiscordBot/.github/workflows/NEW-ACTION-DISCORD-RELEASE.md](../../../DiscordBot/.github/workflows/NEW-ACTION-DISCORD-RELEASE.md)

The action is implemented at the **root of the DiscordBot repo** so other repos can use it with `uses: Owner/DiscordBot@main` (or `@v1`). SunhavenMod’s build-release-publish workflow uses that reference for both the real-release Discord notification and the test-mode notification step.
