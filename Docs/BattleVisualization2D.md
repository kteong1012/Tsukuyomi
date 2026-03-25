# 2D Battle Visualization (World-Space Sprite)

This project now supports a world-space 2D battle layer for AutoChess, while keeping gameplay logic in `Application` and UI flow in `UI Toolkit`.

## Runtime Flow

1. `AutoChessGameService` executes battle simulation.
2. Service writes structured replay data into `AutoChessSnapshot.battleReplay`.
3. `AutoChessBattleWorldController` listens to snapshot changes and plays replay using Sprite world objects.
4. Camera movement is driven by `AutoChessCameraDirector`.

## Art Integration Contract

All unit portraits/characters are loaded from `Resources` paths configured in:

- `Assets/_Project/Config/battle_view.json`
- field: `unitVisuals[].spriteResourcePath`

Current expected default import locations:

- `Assets/_Project/Resources/Art/Units/swordsman.png`
- `Assets/_Project/Resources/Art/Units/mage.png`
- `Assets/_Project/Resources/Art/Units/priest.png`
- `Assets/_Project/Resources/Art/Units/berserker.png`

Fallback if missing:

1. Try `fallbackVisual.spriteResourcePath`
2. If still missing, generate a runtime placeholder square sprite

This means you can import textures later without code changes, and rendering will switch automatically.

## Extension Notes

- For future VFX-heavy combat, keep replay event schema stable and extend only with additive fields.
- Current replay supports action types:
  - `Attack`
  - `SkillDamage`
  - `SkillHeal`
  - `SkillBuff`
- Combat log text remains separate from replay events and can be replaced by richer visualization later.
