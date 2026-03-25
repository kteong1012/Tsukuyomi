# AutoChess AI Workflow

This project keeps a strict AI-first loop where design requests are expressed in text/JSON and then translated into game content by code changes.

## Request Contract

When you ask AI to design or iterate units/skills, include all of the following:

1. `goal`: one sentence about the desired meta or gameplay identity.
2. `skills`: each skill's trigger/effect/power/mana/duration.
3. `units`: each unit's cost, stat line, mana gain, and linked skill.
4. `waves`: enemy compositions for at least the first 3 rounds.
5. `expectedUiState`: what the player should see in shop/board/bench after a key action.
6. `acceptanceCases`: executable checks (example: "buy mage then place on board and start battle, round increments to 2").

Use `autochess_design_request.template.json` in the same folder as the request skeleton.

## Files AI Should Edit

1. `Assets/_Project/Config/autochess_content.json`
2. `Assets/_Project/ConfigSchema/autochess_content.schema.json` (only when adding new fields or effect types)
3. `Assets/_Project/Scripts/Application/AutoChess/*.cs` (only when mechanics need new runtime behavior)
4. `Assets/_Project/Scripts/Presentation/UI/AutoChessViewBinder.cs` (only when UI interaction changes)

## Validation Pipeline

Run these commands after each AI content update:

```bash
python Tools/dev.py validate-config
python Tools/dev.py generate-config
python Tools/dev.py guard-architecture
dotnet clean Tsukuyomi.sln
dotnet build Tsukuyomi.sln
```

What this guarantees:

1. JSON schema is valid.
2. AutoChess semantic rules are valid (unique IDs, valid cross-references, legal numeric ranges).
3. Generated DTOs and runtime config copies are synchronized.
4. Layer boundaries stay clean.

## Hot Reload Notes

In Unity Editor play mode, changing `Assets/_Project/Config/autochess_content.json` will trigger hot reload events.

1. Unit/skill/wave changes apply to the current run.
2. Board/bench/shop slot size changes are resized automatically.
3. If a referenced unit/skill is removed, invalid owned units are cleared safely.

For large balance changes, starting a new run is still recommended for deterministic verification.
