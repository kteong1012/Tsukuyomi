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

        private IUiNavigator _navigator;
        private Label _buildLabel;
        private Button _startButton;
        private Button _settingsButton;
        private Button _fallbackButton;
        private Button _quitButton;

        public MainMenuViewBinder(IGameSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public ScreenId ScreenId => ScreenId.MainMenu;

        public void Bind(IUiElementQuery query, IUiNavigator navigator)
        {
            _navigator = navigator;
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
            Refresh();
        }

        public void Refresh()
        {
            if (_buildLabel != null && _settingsService.Data != null)
            {
                _buildLabel.text = $"Build: {_settingsService.Data.buildLabel}";
            }
        }

        public void Unbind()
        {
            _settingsService.Changed -= OnSettingsChanged;

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

        private void OnStart()
        {
            Debug.Log("Start requested. Replace this with gameplay scene loading.");
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
