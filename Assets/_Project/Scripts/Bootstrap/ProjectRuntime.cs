using Tsukuyomi.Application.Settings;
using Tsukuyomi.Application.UI;

namespace Tsukuyomi.Bootstrap
{
    public static class ProjectRuntime
    {
        public static IUiNavigator Navigator { get; private set; }

        public static IGameSettingsService SettingsService { get; private set; }

        public static void Initialize(IUiNavigator navigator, IGameSettingsService settingsService)
        {
            Navigator = navigator;
            SettingsService = settingsService;
        }

        public static void Reset()
        {
            Navigator = null;
            SettingsService = null;
        }
    }
}
