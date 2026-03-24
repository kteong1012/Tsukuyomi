# Tsukuyomi

AI-first Unity project scaffold with script-driven architecture, JSON-first configuration, and UI Toolkit as the default runtime UI stack.

## Quick Start
- Validate configuration:
  - `python Tools/dev.py validate-config`
- Generate config DTOs and sync runtime copies:
  - `python Tools/dev.py generate-config`
- Check generated files are up-to-date:
  - `python Tools/dev.py generate-config --check`
- Run architecture guard checks:
  - `python Tools/dev.py guard-architecture`
- Run gate suite:
  - `python Tools/dev.py run-tests`
- Run smoke UI tests:
  - `python Tools/dev.py smoke-ui`

## UI Strategy
- Default runtime UI: `UI Toolkit` (`UXML + USS + C# binders`)
- Controlled fallback: `uGUI` whitelist only (`ScreenId.UguiFallbackDemo`)
- Runtime composition is code-driven through `ProjectCompositionRoot`

## Configuration Strategy
- Source JSON: `Assets/_Project/Config/*.json`
- JSON Schema: `Assets/_Project/ConfigSchema/*.schema.json`
- Generated C#: `Assets/_Project/Scripts/Generated/Config/*.g.cs`
- Runtime config copies: `Assets/_Project/Resources/Config/*.json`

## Layering
- `Tsukuyomi.Domain`
- `Tsukuyomi.Application`
- `Tsukuyomi.Infrastructure`
- `Tsukuyomi.Presentation`
- `Tsukuyomi.Bootstrap`
