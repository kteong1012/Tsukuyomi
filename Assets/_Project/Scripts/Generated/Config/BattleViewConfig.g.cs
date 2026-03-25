using System;

namespace Tsukuyomi.Generated.Config
{
    [Serializable]
    public sealed class BattleWorldConfig
    {
        public float boardCenterX = 0.0f;
        public float playerRowY = -2.0f;
        public float enemyRowY = 2.0f;
        public float laneSpacingX = 1.25f;
        public float benchRowY = -3.4f;
        public float unitScale = 1.0f;
        public float attackHopDistance = 0.5f;
        public float attackHopDuration = 0.12f;
        public float turnInterval = 0.34f;
        public float hitFlashDuration = 0.08f;
        public float postBattleHold = 0.8f;
    }

    [Serializable]
    public sealed class CameraRigConfig
    {
        public float positionX = 0.0f;
        public float positionY = 0.0f;
        public float positionZ = -10.0f;
        public float orthographicSize = 5.4f;
        public float battleZoomSize = 4.8f;
        public float smoothTime = 0.16f;
        public float shakeAmplitude = 0.12f;
        public float shakeDuration = 0.14f;
    }

    [Serializable]
    public sealed class UnitVisualsItemConfig
    {
        public string unitId = "";
        public string spriteResourcePath = "";
    }

    [Serializable]
    public sealed class FallbackVisualConfig
    {
        public string spriteResourcePath = "Art/Units/fallback_unit";
        public string tintHex = "#A9B6CC";
    }

    [Serializable]
    public sealed class BattleViewConfig
    {
        public BattleWorldConfig battleWorld = new BattleWorldConfig();
        public CameraRigConfig cameraRig = new CameraRigConfig();
        public UnitVisualsItemConfig[] unitVisuals = System.Array.Empty<UnitVisualsItemConfig>();
        public FallbackVisualConfig fallbackVisual = new FallbackVisualConfig();
    }
}
