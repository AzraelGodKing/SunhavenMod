# Mod Lifecycle and Logging Contract

Shared contract for all Sun Haven mods in this repository.

## 1) Lifecycle Contract

Every mod must follow these rules:

- Subscribe/unsubscribe symmetry:
  - if a handler is subscribed in `Awake`/init, it must be unsubscribed in teardown.
- Save/load ownership must be deterministic:
  - one clear owner for loading runtime state
  - one clear owner for persistence writes
- Scene transitions must not reset or overwrite state unless explicitly intended.
- Character switches must rebind runtime state exactly once.
- Destruction during menu/app quit should not be treated as world-object deletion side effects.

## 2) Persistence Safety Contract

- Never overwrite known-good non-empty persisted data with empty runtime state unless explicitly confirmed valid for current session.
- Use atomic writes (`.tmp` -> final) when writing save files.
- Prefer backup file fallback for deserialize failures.
- Log recoverable persistence anomalies at warning level and include enough context for support triage.

## 3) Logging Contract

- `Info`: user-relevant milestones only (startup complete, data loaded, major actions).
- `Warning`: degraded behavior or fallback path activated.
- `Error`: operation failed and likely needs intervention.
- High-frequency loops/timers must be debug-gated behind config toggles.

## 4) Integration Contract (Soft Dependencies)

- Integration layers must be optional and resilient:
  - absent dependency must not break startup
  - missing methods/types must degrade gracefully
- Event subscriptions to external managers must be disposable and cleaned in teardown.
- Integration status should be summarized once at startup.

## 5) Support Diagnostics Contract

Each mod should provide:

- one startup health summary line (versions, key integrations, mode)
- optional debug diagnostics dump containing:
  - active character identity
  - data loaded state
  - integration status
  - last persistence operation outcome

This keeps support logs consistent across mods and reduces triage time.

