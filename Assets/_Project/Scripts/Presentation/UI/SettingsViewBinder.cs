using Tsukuyomi.Application.Settings;
using Tsukuyomi.Application.UI;
using Tsukuyomi.Domain.UI;
using UnityEngine.UIElements;

namespace Tsukuyomi.Presentation.UI
{
    public sealed class SettingsViewBinder : IUiViewBinder
    {
        private readonly IGameSettingsService _settingsService;

        private IUiNavigator _navigator;
        private Slider _masterVolumeSlider;
        private Toggle _fullscreenToggle;
        private Button _closeButton;

        private EventCallback<ChangeEvent<float>> _masterVolumeChangedCallback;
        private EventCallback<ChangeEvent<bool>> _fullscreenChangedCallback;

        public SettingsViewBinder(IGameSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public ScreenId ScreenId => ScreenId.Settings;

        public void Bind(IUiElementQuery query, IUiNavigator navigator)
        {
            _navigator = navigator;
            _masterVolumeSlider = query.Q<Slider>("master-volume-slider");
            _fullscreenToggle = query.Q<Toggle>("fullscreen-toggle");
            _closeButton = query.Q<Button>("back-btn");

            _masterVolumeChangedCallback = OnMasterVolumeChanged;
            _fullscreenChangedCallback = OnFullscreenChanged;

            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.RegisterValueChangedCallback(_masterVolumeChangedCallback);
            }

            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.RegisterValueChangedCallback(_fullscreenChangedCallback);
            }

            if (_closeButton != null)
            {
                _closeButton.clicked += OnClose;
            }

            _settingsService.Changed += OnSettingsChanged;
            Refresh();
        }

        public void Refresh()
        {
            if (_settingsService.Data == null)
            {
                return;
            }

            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.SetValueWithoutNotify(_settingsService.Data.audio.masterVolume);
            }

            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.SetValueWithoutNotify(_settingsService.Data.video.fullscreen);
            }
        }

        public void Unbind()
        {
            _settingsService.Changed -= OnSettingsChanged;

            if (_masterVolumeSlider != null && _masterVolumeChangedCallback != null)
            {
                _masterVolumeSlider.UnregisterValueChangedCallback(_masterVolumeChangedCallback);
            }

            if (_fullscreenToggle != null && _fullscreenChangedCallback != null)
            {
                _fullscreenToggle.UnregisterValueChangedCallback(_fullscreenChangedCallback);
            }

            if (_closeButton != null)
            {
                _closeButton.clicked -= OnClose;
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

        private void OnMasterVolumeChanged(ChangeEvent<float> evt)
        {
            _settingsService.SetMasterVolume(evt.newValue);
        }

        private void OnFullscreenChanged(ChangeEvent<bool> evt)
        {
            _settingsService.SetFullscreen(evt.newValue);
        }

        private void OnClose()
        {
            _navigator.HideOverlay(ScreenId.Settings);
        }
    }
}
