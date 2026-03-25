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
- Full compile pass:
  - `dotnet clean Tsukuyomi.sln`
  - `dotnet build Tsukuyomi.sln`

## UI Strategy
- Default runtime UI: `UI Toolkit` (`UXML + USS + C# binders`)
- Controlled fallback: `uGUI` whitelist only (`ScreenId.UguiFallbackDemo`)
- Runtime composition is code-driven through `ProjectCompositionRoot`

## Configuration Strategy
- Source JSON: `Assets/_Project/Config/*.json`
- JSON Schema: `Assets/_Project/ConfigSchema/*.schema.json`
- Generated C#: `Assets/_Project/Scripts/Generated/Config/*.g.cs`
- Runtime config copies: `Assets/_Project/Resources/Config/*.json`
- Localization source: `Assets/_Project/Config/localization_texts.json` (`en` / `zh-Hans`)
- Battle world view source: `Assets/_Project/Config/battle_view.json`

## Layering
- `Tsukuyomi.Domain`
- `Tsukuyomi.Application`
- `Tsukuyomi.Infrastructure`
- `Tsukuyomi.Presentation`
- `Tsukuyomi.Bootstrap`

## AutoChess + AI Workflow
- Runtime content file:
  - `Assets/_Project/Config/autochess_content.json`
- AI request template:
  - `Docs/autochess_design_request.template.json`
- Workflow guide:
  - `Docs/AutoChessAIWorkflow.md`

Recommended loop:
1. Write requirements in the request template (goal, skills, units, waves, expected UI state, acceptance cases).
2. Ask AI to apply them by updating `autochess_content.json` (and schema/code only when needed).
3. Run `python Tools/dev.py validate-config` and `python Tools/dev.py generate-config`.
4. Run `python Tools/dev.py guard-architecture` and `dotnet build Tsukuyomi.sln`.

## Battle 2D World
- Visual integration guide:
  - `Docs/BattleVisualization2D.md`
- Camera system design:
  - `Docs/CameraSystem.md`
- Sprite drop location:
  - `Assets/_Project/Resources/Art/Units`
