using NUnit.Framework;
using Tsukuyomi.Generated.Config;
using UnityEngine;

namespace Tsukuyomi.Tests.EditMode
{
    public sealed class GameSettingsConfigRoundtripTests
    {
        [Test]
        public void JsonRoundtrip_PreservesCriticalFields()
        {
            var source = new GameSettingsConfig
            {
                buildLabel = "test-build",
                audio = new AudioConfig
                {
                    masterVolume = 0.42f,
                    musicVolume = 0.5f,
                    sfxVolume = 0.7f
                },
                video = new VideoConfig
                {
                    fullscreen = false,
                    resolution = "1280x720",
                    targetFrameRate = 120
                },
                ui = new UiConfig
                {
                    defaultScreen = "MainMenu",
                    theme = "day"
                }
            };

            var json = JsonUtility.ToJson(source);
            var copy = JsonUtility.FromJson<GameSettingsConfig>(json);

            Assert.That(copy.buildLabel, Is.EqualTo(source.buildLabel));
            Assert.That(copy.audio.masterVolume, Is.EqualTo(source.audio.masterVolume).Within(0.0001f));
            Assert.That(copy.video.fullscreen, Is.EqualTo(source.video.fullscreen));
            Assert.That(copy.video.targetFrameRate, Is.EqualTo(source.video.targetFrameRate));
        }
    }
}
