# Security

## Reporting issues

- **Save corruption / unintended data loss** in shipped mods: open a **private** GitHub security advisory or contact the repository maintainer with logs (`LogOutput.log`), steps, and mod versions from `docs/versions.json`.
- **Sensitive credentials** (tokens, API keys): rotate them immediately; do **not** paste secrets into issues.

## Expectations

These are game mods (BepInEx plugins). They run with full local user privileges and are not a sandbox. Treat in-repo crypto as **tamper-resistant storage**, not protection against a motivated local attacker.
