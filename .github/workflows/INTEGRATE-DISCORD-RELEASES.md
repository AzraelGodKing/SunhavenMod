# Discord release notifications (integrated)

Release notifications to Discord use the **Discord_Release** repo action and are wired in [build-release-publish.yml](build-release-publish.yml).

## How it works

- **Real release:** When you run the workflow (manual dispatch) and create a GitHub Release, GitHub emits `release: published`. The **notify_discord** job runs and calls `AzraelGodKing/Discord_Release@main` with no title/body so the action uses the release event and posts the changelog to Discord.
- **Test mode:** Check **"Test Discord notification only"** when running the workflow. The workflow uses fake mod data, skips GitHub Release and Thunderstore, and runs the same Discord_Release action with `title` and `body` to post a test message. Use this to confirm the webhook and channel without publishing a release.

## Action

- **Repo:** [Discord_Release](https://github.com/AzraelGodKing/Discord_Release) (or your fork). Replace `AzraelGodKing/Discord_Release@main` in the workflow with your repo and ref (e.g. `@v1`).
- **Secret:** Add **DISCORD_WEBHOOK_URL** in SunhavenMod repo → Settings → Secrets and variables → Actions (your Discord channel webhook URL).

## Workflow summary

- **Trigger:** `workflow_dispatch` (existing inputs + `test_discord` boolean) and `release: types: [published]`.
- **setup** / **release** jobs run only on `workflow_dispatch`. When `test_discord` is true, matrix is `["test_discord"]`, config uses fake data, version check is skipped, package step creates a minimal zip, GitHub Release and Thunderstore are skipped, and "Post test notification to Discord" runs with the Discord_Release action.
- **notify_discord** job runs only on `release: published` and uses the Discord_Release action with the event payload.
