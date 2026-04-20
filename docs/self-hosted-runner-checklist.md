# Self-Hosted Runner Checklist (SunhavenMod)

Use this checklist before enabling self-hosted build workflows.

## Host Setup

- Docker Desktop installed on Windows host.
- Docker set to Windows containers mode.
- Docker service starts on boot.
- GitHub repository admin access available for runner registration.

## Required Host Folders

- `C:\ci\sunhaven-runner\config`
- `C:\ci\sunhaven-runner\work`
- `C:\ci\sunhaven-refs\Sun Haven_Data\Managed`

## Copy Sun Haven References

Copy required managed references from:

- `C:\Program Files (x86)\Steam\steamapps\common\Sun Haven\Sun Haven_Data\Managed`

Into:

- `C:\ci\sunhaven-refs\Sun Haven_Data\Managed`

Minimum required DLLs:

- `Assembly-CSharp.dll`
- `BepInEx.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.dll`
- `Unity.TextMeshPro.dll`
- `mscorlib.dll`
- `System.dll`
- `System.Core.dll`

## Runner Compose Config

- Open `.github/self-hosted-runner/docker-compose.windows-runner.yml`
- Set:
  - `GITHUB_REPOSITORY_URL`
  - `RUNNER_TOKEN` (fresh ephemeral token)
  - `RUNNER_NAME` (unique)
- Confirm volume mounts and `:ro` on refs mount.

## Start and Validate

- Start: `docker compose -f .github/self-hosted-runner/docker-compose.windows-runner.yml up -d --build`
- Confirm runner is Online in GitHub Actions Runners page.
- Run preflight check:
  - `pwsh ./scripts/check-sunhaven-refs.ps1 -SunHavenPath "C:\ci\sunhaven-refs"`

## Pre-Release Validation

- Run version drift check:
  - `pwsh ./scripts/check-version-drift.ps1`
- Run build script for one mod:
  - `pwsh ./scripts/pre-push-build.ps1 -Mod senpaischest -BuildOnly`
- Confirm output DLL exists in expected build output location.

## Ongoing Maintenance

- Refresh `sunhaven-refs` after game updates.
- Rotate runner tokens as needed.
- Keep runner image + .NET SDK updated periodically.
