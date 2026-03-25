using System;

namespace Tsukuyomi.Generated.Config
{
    [Serializable]
    public sealed class UiConfig
    {
        public string defaultScreen = "MainMenu";
        public string theme = "day";
        public string language = "zh-Hans";
    }

    [Serializable]
    public sealed class AudioConfig
    {
        public float masterVolume = 0.85f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.8f;
    }

    [Serializable]
    public sealed class VideoConfig
    {
        public bool fullscreen = true;
        public string resolution = "1920x1080";
        public int targetFrameRate = 60;
    }

    [Serializable]
    public sealed class GameSettingsConfig
    {
        public string buildLabel = "prototype-001";
        public UiConfig ui = new UiConfig();
        public AudioConfig audio = new AudioConfig();
        public VideoConfig video = new VideoConfig();
    }
}
