using System;

namespace Tsukuyomi.Generated.Config
{
    [Serializable]
    public sealed class LanguagesItemConfig
    {
        public string code = "";
        public string displayName = "";
    }

    [Serializable]
    public sealed class EntriesItemConfig
    {
        public string key = "";
        public string en = "";
        public string zhHans = "";
    }

    [Serializable]
    public sealed class LocalizationTextsConfig
    {
        public string defaultLanguage = "zh-Hans";
        public LanguagesItemConfig[] languages = System.Array.Empty<LanguagesItemConfig>();
        public EntriesItemConfig[] entries = System.Array.Empty<EntriesItemConfig>();
    }
}
