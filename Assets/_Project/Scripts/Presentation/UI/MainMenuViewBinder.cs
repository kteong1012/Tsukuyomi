using Tsukuyomi.Application.Localization;
using Tsukuyomi.Application.Settings;
using Tsukuyomi.Application.UI;
using Tsukuyomi.Domain.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tsukuyomi.Presentation.UI
{
    public sealed class MainMenuViewBinder : IUiViewBinder
    {
        private readonly IGameSettingsService _settingsService;
        private readonly ILocalizationService _localizationService;

        private IUiNavigator _navigator;
        private Label _titleLabel;
        private Label _buildLabel;
        private Button _startButton;
        private Button _settingsButton;
        private Button _fallbackButton;
        private Button _quitButton;

        public MainMenuViewBinder(
            IGameSettingsService settingsService,
            ILocalizationService localizationService)
        {
            _settingsService = settingsService;
            _localizationService = localizationService;
        }

        public ScreenId ScreenId => ScreenId.MainMenu;

        public void Bind(IUiElementQuery query, IUiNavigator navigator)
        {
            _navigator = navigator;
            _titleLabel = query.Q<Label>("title-label");
            _buildLabel = query.Q<Label>("build-label");
            _startButton = query.Q<Button>("start-btn");
            _settingsButton = query.Q<Button>("settings-btn");
            _fallbackButton = query.Q<Button>("ugui-fallback-btn");
            _quitButton = query.Q<Button>("quit-btn");

            if (_startButton != null)
            {
                _startButton.clicked += OnStart;
            }

            if (_settingsButton != null)
            {
                _settingsButton.clicked += OnSettings;
            }

            if (_fallbackButton != null)
            {
                _fallbackButton.clicked += OnFallback;
            }

            if (_quitButton != null)
            {
                _quitButton.clicked += OnQuit;
            }

            _settingsService.Changed += OnSettingsChanged;
            _localizationService.Changed += OnLocalizationChanged;
            Refresh();
        }

        public void Refresh()
        {
            if (_titleLabel != null)
            {
                _titleLabel.text = _localizationService.GetText("ui.main.title");
            }

            if (_buildLabel != null && _settingsService.Data != null)
            {
                var buildPrefix = _localizationService.GetText("ui.main.buildPrefix");
                _buildLabel.text = $"{buildPrefix}: {_settingsService.Data.buildLabel}";
            }

            if (_startButton != null)
            {
                _startButton.text = _localizationService.GetText("ui.main.start");
            }

            if (_settingsButton != null)
            {
                _settingsButton.text = _localizationService.GetText("ui.main.settings");
            }

            if (_fallbackButton != null)
            {
                _fallbackButton.text = _localizationService.GetText("ui.main.fallback");
            }

            if (_quitButton != null)
            {
                _quitButton.text = _localizationService.GetText("ui.main.quit");
            }
        }

        public void Unbind()
        {
            _settingsService.Changed -= OnSettingsChanged;
            _localizationService.Changed -= OnLocalizationChanged;

            if (_startButton != null)
            {
                _startButton.clicked -= OnStart;
            }

            if (_settingsButton != null)
            {
                _settingsButton.clicked -= OnSettings;
            }

            if (_fallbackButton != null)
            {
                _fallbackButton.clicked -= OnFallback;
            }

            if (_quitButton != null)
            {
                _quitButton.clicked -= OnQuit;
            }
        }

        public void Dispose()
        {
            Unbind();
        }

        private void OnSettingsChanged()
        {
            Refresh();
        }

        private void OnLocalizationChanged()
        {
            Refresh();
        }

        private void OnStart()
        {
            _navigator.Replace(ScreenId.AutoChess);
        }

        private void OnSettings()
        {
            _navigator.ShowOverlay(ScreenId.Settings);
        }

        private void OnFallback()
        {
            if (_navigator.IsVisible(ScreenId.UguiFallbackDemo))
            {
                _navigator.HideOverlay(ScreenId.UguiFallbackDemo);
            }
            else
            {
                _navigator.ShowOverlay(ScreenId.UguiFallbackDemo);
            }
        }

        private static void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
