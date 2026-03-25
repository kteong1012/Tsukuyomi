using Tsukuyomi.Application.AutoChess;
using Tsukuyomi.Application.Localization;
using Tsukuyomi.Application.Settings;
using Tsukuyomi.Application.UI;

namespace Tsukuyomi.Bootstrap
{
    public static class ProjectRuntime
    {
        public static IUiNavigator Navigator { get; private set; }

        public static IGameSettingsService SettingsService { get; private set; }

        public static IAutoChessGameService AutoChessGameService { get; private set; }

        public static ILocalizationService LocalizationService { get; private set; }

        public static void Initialize(
            IUiNavigator navigator,
            IGameSettingsService settingsService,
            IAutoChessGameService autoChessGameService,
            ILocalizationService localizationService)
        {
            Navigator = navigator;
            SettingsService = settingsService;
            AutoChessGameService = autoChessGameService;
            LocalizationService = localizationService;
        }

        public static void Reset()
        {
            Navigator = null;
            SettingsService = null;
            AutoChessGameService = null;
            LocalizationService = null;
        }
    }
}
