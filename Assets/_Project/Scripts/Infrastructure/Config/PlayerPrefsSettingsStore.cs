using Tsukuyomi.Application.Settings;
using UnityEngine;

namespace Tsukuyomi.Infrastructure.Config
{
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        private readonly string _prefix;

        public PlayerPrefsSettingsStore(string prefix)
        {
            _prefix = prefix ?? string.Empty;
        }

        public bool TryGetFloat(string key, out float value)
        {
            var fullKey = $"{_prefix}{key}";
            if (!PlayerPrefs.HasKey(fullKey))
            {
                value = default;
                return false;
            }

            value = PlayerPrefs.GetFloat(fullKey);
            return true;
        }

        public bool TryGetBool(string key, out bool value)
        {
            var fullKey = $"{_prefix}{key}";
            if (!PlayerPrefs.HasKey(fullKey))
            {
                value = default;
                return false;
            }

            value = PlayerPrefs.GetInt(fullKey) == 1;
            return true;
        }

        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat($"{_prefix}{key}", value);
        }

        public void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt($"{_prefix}{key}", value ? 1 : 0);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
