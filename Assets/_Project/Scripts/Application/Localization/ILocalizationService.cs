using System;
using System.Collections.Generic;

namespace Tsukuyomi.Application.Localization
{
    public sealed class LanguageOption
    {
        public string code = string.Empty;
        public string displayName = string.Empty;
    }

    public interface ILocalizationService
    {
        event Action Changed;

        string CurrentLanguageCode { get; }

        string DefaultLanguageCode { get; }

        IReadOnlyList<LanguageOption> Languages { get; }

        string GetText(string key);

        string Format(string key, params object[] args);

        void SetLanguage(string languageCode);
    }
}
