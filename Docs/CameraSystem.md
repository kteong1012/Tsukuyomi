# Camera System Design

## Goals

1. Keep camera behavior deterministic and code-driven.
2. Decouple camera logic from battle simulation.
3. Allow future swap to Cinemachine with minimal gameplay-side changes.

## Current Implementation

- Driver: `AutoChessCameraDirector`
- Host: `AutoChessBattleWorldController`
- Config source: `Assets/_Project/Config/battle_view.json` -> `cameraRig`

### Camera Modes

1. `Board View`
   - Uses configured base position + `orthographicSize`
   - Applied when idle / formation display
2. `Battle Focus`
   - Focuses midpoint between actor and target
   - Uses `battleZoomSize`
3. `Pulse Shake`
   - Short procedural shake on each replay event
   - Controlled by `shakeAmplitude` / `shakeDuration`

### Smoothing

- Position and size use damped interpolation (`SmoothDamp`)
- Smoothing speed controlled by `smoothTime`

## Configuration Fields

`cameraRig` currently includes:

1. `positionX`, `positionY`, `positionZ`
2. `orthographicSize`
3. `battleZoomSize`
4. `smoothTime`
5. `shakeAmplitude`
6. `shakeDuration`

## Cinemachine Upgrade Path (Optional)

Current system does not require Cinemachine.

If you want advanced camera blending and virtual camera tracks later:

1. Install Cinemachine package via Unity Package Manager.
2. Add a Cinemachine adapter that implements the same "Board View / Battle Focus / Pulse" API shape.
3. Keep `AutoChessBattleWorldController` unchanged except the camera driver instantiation line.

This keeps battle logic and replay playback untouched while upgrading camera quality.
