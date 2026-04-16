# Docker .NET Builder Container

This repo includes a local Docker image with the .NET SDK preinstalled.

## Files

- `Dockerfile.dotnet-sdk`
- `docker-compose.dotnet-sdk.yml`
- `.env.dotnet-sdk.example`

## Quick start

1. Copy env template:
   - PowerShell: `Copy-Item .env.dotnet-sdk.example .env`
   - Bash: `cp .env.dotnet-sdk.example .env`
2. Build image:
   - `docker compose -f docker-compose.dotnet-sdk.yml build`
3. Start shell inside container:
   - `docker compose -f docker-compose.dotnet-sdk.yml run --rm dotnet-builder`

## Build a mod inside the container

From the container shell:

```bash
dotnet --info
dotnet restore HavensBirthright/HavensBirthright.csproj
dotnet build HavensBirthright/HavensBirthright.csproj -c Release
```

## Notes

- This image is for local development/build commands.
- `net48` projects may need Windows-specific tooling in some scenarios. If a project fails in Linux containers, use a Windows host/runner for that build.
