using System;
using NUnit.Framework;
using Tsukuyomi.Application.Config;
using Tsukuyomi.Application.Localization;
using Tsukuyomi.Application.Settings;
using Tsukuyomi.Generated.Config;

namespace Tsukuyomi.Tests.EditMode
{
    public sealed class LocalizationServiceTests
    {
        [Test]
        public void GetText_ReturnsTextForCurrentLanguage()
        {
            var repository = new FakeConfigRepository(BuildConfig());
            var hotReload = new FakeHotReloadService();
            var settings = new FakeSettingsService("en");
            var service = new LocalizationService(repository, hotReload, settings);

            Assert.That(service.GetText("ui.main.start"), Is.EqualTo("Start Game"));

            settings.SetLanguage("zh-Hans");
            Assert.That(service.GetText("ui.main.start"), Is.EqualTo("开始游戏"));
        }

        [Test]
        public void ConfigHotReload_UpdatesEntries()
        {
            var repository = new FakeConfigRepository(BuildConfig());
            var hotReload = new FakeHotReloadService();
            var settings = new FakeSettingsService("en");
            var service = new LocalizationService(repository, hotReload, settings);

            var changed = false;
            service.Changed += () => changed = true;

            var updated = BuildConfig();
            updated.entries[0].en = "Play";
            repository.Set(updated);
            hotReload.Raise("localization_texts");

            Assert.That(changed, Is.True);
            Assert.That(service.GetText("ui.main.start"), Is.EqualTo("Play"));
        }

        private static LocalizationTextsConfig BuildConfig()
        {
            return new LocalizationTextsConfig
            {
                defaultLanguage = "en",
                languages = new[]
                {
                    new LanguagesItemConfig { code = "en", displayName = "English" },
                    new LanguagesItemConfig { code = "zh-Hans", displayName = "简体中文" }
                },
                entries = new[]
                {
                    new EntriesItemConfig
                    {
                        key = "ui.main.start",
                        en = "Start Game",
                        zhHans = "开始游戏"
                    }
                }
            };
        }

        private sealed class FakeConfigRepository : IConfigRepository<LocalizationTextsConfig>
        {
            private LocalizationTextsConfig _content;

            public FakeConfigRepository(LocalizationTextsConfig content)
            {
                _content = content;
            }

            public string ConfigName => "localization_texts";

            public LocalizationTextsConfig Get()
            {
                return _content;
            }

            public LocalizationTextsConfig Reload()
            {
                return _content;
            }

            public string GetRawJson()
            {
                return "{}";
            }

            public void Set(LocalizationTextsConfig next)
            {
                _content = next;
            }
        }

        private sealed class FakeHotReloadService : IConfigHotReloadService
        {
            public event Action<string> Reloaded;

            public void Start()
            {
            }

            public void Stop()
            {
            }

            public void Dispose()
            {
            }

            public void Raise(string configName)
            {
                Reloaded?.Invoke(configName);
            }
        }

        private sealed class FakeSettingsService : IGameSettingsService
        {
            private readonly GameSettingsConfig _data;

            public FakeSettingsService(string languageCode)
            {
                _data = new GameSettingsConfig
                {
                    ui = new UiConfig
                    {
                        defaultScreen = "MainMenu",
                        theme = "day",
                        language = languageCode
                    }
                };
            }

            public event Action Changed;

            public GameSettingsConfig Data => _data;

            public string CurrentLanguageCode => _data.ui.language;

            public void Reload()
            {
                Changed?.Invoke();
            }

            public void SetMasterVolume(float value)
            {
            }

            public void SetFullscreen(bool enabled)
            {
            }

            public void SetLanguage(string languageCode)
            {
                _data.ui.language = languageCode;
                Changed?.Invoke();
            }
        }
    }
}
