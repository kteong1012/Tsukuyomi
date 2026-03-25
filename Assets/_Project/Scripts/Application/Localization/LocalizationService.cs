using System;
using System.Collections.Generic;
using Tsukuyomi.Application.Config;
using Tsukuyomi.Application.Settings;
using Tsukuyomi.Generated.Config;

namespace Tsukuyomi.Application.Localization
{
    public sealed class LocalizationService : ILocalizationService
    {
        private readonly IConfigRepository<LocalizationTextsConfig> _repository;
        private readonly IConfigHotReloadService _hotReloadService;
        private readonly IGameSettingsService _settingsService;

        private readonly Dictionary<string, EntriesItemConfig> _entryByKey =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly List<LanguageOption> _languages = new();

        private string _defaultLanguageCode = "en";
        private string _currentLanguageCode;

        public LocalizationService(
            IConfigRepository<LocalizationTextsConfig> repository,
            IConfigHotReloadService hotReloadService,
            IGameSettingsService settingsService)
        {
            _repository = repository;
            _hotReloadService = hotReloadService;
            _settingsService = settingsService;

            _hotReloadService.Reloaded += OnConfigReloaded;
            _settingsService.Changed += OnSettingsChanged;

            ReloadContent();
            _currentLanguageCode = NormalizeLanguageCode(_settingsService.CurrentLanguageCode);
        }

        public event Action Changed;

        public string CurrentLanguageCode => NormalizeLanguageCode(_settingsService.CurrentLanguageCode);

        public string DefaultLanguageCode => _defaultLanguageCode;

        public IReadOnlyList<LanguageOption> Languages => _languages;

        public string GetText(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            if (!_entryByKey.TryGetValue(key, out var entry))
            {
                return key;
            }

            var language = CurrentLanguageCode;
            if (string.Equals(language, "zh-Hans", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(entry.zhHans))
                {
                    return entry.zhHans;
                }

                if (!string.IsNullOrWhiteSpace(entry.en))
                {
                    return entry.en;
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(entry.en))
                {
                    return entry.en;
                }

                if (!string.IsNullOrWhiteSpace(entry.zhHans))
                {
                    return entry.zhHans;
                }
            }

            if (string.Equals(_defaultLanguageCode, "zh-Hans", StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrWhiteSpace(entry.zhHans)
                    ? entry.zhHans
                    : key;
            }

            return !string.IsNullOrWhiteSpace(entry.en)
                ? entry.en
                : key;
        }

        public string Format(string key, params object[] args)
        {
            var format = GetText(key);
            if (args == null || args.Length == 0)
            {
                return format;
            }

            return string.Format(format, args);
        }

        public void SetLanguage(string languageCode)
        {
            _settingsService.SetLanguage(NormalizeLanguageCode(languageCode));
        }

        private void ReloadContent()
        {
            var content = _repository.Reload() ?? new LocalizationTextsConfig();
            _defaultLanguageCode = NormalizeLanguageCode(content.defaultLanguage);

            _entryByKey.Clear();
            if (content.entries != null)
            {
                for (var i = 0; i < content.entries.Length; i++)
                {
                    var entry = content.entries[i];
                    if (string.IsNullOrWhiteSpace(entry.key))
                    {
                        continue;
                    }

                    _entryByKey[entry.key] = entry;
                }
            }

            _languages.Clear();
            if (content.languages != null)
            {
                for (var i = 0; i < content.languages.Length; i++)
                {
                    var item = content.languages[i];
                    var code = NormalizeLanguageCode(item.code);
                    if (ContainsLanguage(code))
                    {
                        continue;
                    }

                    _languages.Add(new LanguageOption
                    {
                        code = code,
                        displayName = item.displayName ?? code
                    });
                }
            }

            if (_languages.Count == 0)
            {
                _languages.Add(new LanguageOption { code = "en", displayName = "English" });
                _languages.Add(new LanguageOption { code = "zh-Hans", displayName = "简体中文" });
            }
        }

        private bool ContainsLanguage(string code)
        {
            for (var i = 0; i < _languages.Count; i++)
            {
                if (string.Equals(_languages[i].code, code, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnSettingsChanged()
        {
            var nextLanguage = CurrentLanguageCode;
            if (string.Equals(_currentLanguageCode, nextLanguage, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _currentLanguageCode = nextLanguage;
            Changed?.Invoke();
        }

        private void OnConfigReloaded(string configName)
        {
            if (!string.Equals(configName, _repository.ConfigName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ReloadContent();
            Changed?.Invoke();
        }

        private static string NormalizeLanguageCode(string languageCode)
        {
            if (string.Equals(languageCode, "zh-Hans", StringComparison.OrdinalIgnoreCase))
            {
                return "zh-Hans";
            }

            return "en";
        }
    }
}
