# Tsukuyomi AI-First Development Contract

## Human + AI Request Unit
Every implementation request should include:
1. Goal
2. Input JSON sample
3. Expected UI state
4. Acceptance test scenario

## Architecture Guardrails
- Runtime UI defaults to `UI Toolkit` (`UXML + USS + C# binder`).
- `uGUI` is whitelist-only fallback for unsupported runtime cases.
- Business configuration uses JSON files under `Assets/_Project/Config`.
- ScriptableObject is allowed only for engine-required assets.
- Layering must follow:
  - `Domain`
  - `Application`
  - `Infrastructure`
  - `Presentation`
  - `Bootstrap` (composition only)

## Commands
- Validate configuration:
  - `python Tools/dev.py validate-config`
- Generate and sync configuration artifacts:
  - `python Tools/dev.py generate-config`
- Verify generated artifacts are current:
  - `python Tools/dev.py generate-config --check`
- Enforce architecture policies:
  - `python Tools/dev.py guard-architecture`
- Run full gate:
  - `python Tools/dev.py run-tests`
- Run smoke UI tests:
  - `python Tools/dev.py smoke-ui`

## Runtime Config Flow
1. Edit JSON in `Assets/_Project/Config`.
2. Validate against schema in `Assets/_Project/ConfigSchema`.
3. Generate C# DTOs into `Assets/_Project/Scripts/Generated/Config`.
4. Sync runtime resources into `Assets/_Project/Resources/Config`.

## CI Policy
Each PR should pass:
1. `validate-config`
2. `generate-config --check`
3. `guard-architecture`
4. EditMode and PlayMode tests
5. Smoke UI test filter
