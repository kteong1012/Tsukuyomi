using System.Collections.Generic;
using Tsukuyomi.Application.Localization;
using Tsukuyomi.Application.Settings;
using Tsukuyomi.Application.UI;
using Tsukuyomi.Domain.UI;
using UnityEngine.UIElements;

namespace Tsukuyomi.Presentation.UI
{
    public sealed class SettingsViewBinder : IUiViewBinder
    {
        private readonly IGameSettingsService _settingsService;
        private readonly ILocalizationService _localizationService;

        private IUiNavigator _navigator;
        private Label _titleLabel;
        private Label _masterVolumeLabel;
        private Label _fullscreenLabel;
        private Label _languageLabel;
        private Slider _masterVolumeSlider;
        private Toggle _fullscreenToggle;
        private DropdownField _languageDropdown;
        private Button _closeButton;

        private EventCallback<ChangeEvent<float>> _masterVolumeChangedCallback;
        private EventCallback<ChangeEvent<bool>> _fullscreenChangedCallback;
        private EventCallback<ChangeEvent<string>> _languageChangedCallback;

        private readonly List<string> _languageCodes = new();
        private readonly List<string> _languageLabels = new();

        public SettingsViewBinder(
            IGameSettingsService settingsService,
            ILocalizationService localizationService)
        {
            _settingsService = settingsService;
            _localizationService = localizationService;
        }

        public ScreenId ScreenId => ScreenId.Settings;

        public void Bind(IUiElementQuery query, IUiNavigator navigator)
        {
            _navigator = navigator;
            _titleLabel = query.Q<Label>("settings-title-label");
            _masterVolumeLabel = query.Q<Label>("master-volume-label");
            _fullscreenLabel = query.Q<Label>("fullscreen-label");
            _languageLabel = query.Q<Label>("language-label");
            _masterVolumeSlider = query.Q<Slider>("master-volume-slider");
            _fullscreenToggle = query.Q<Toggle>("fullscreen-toggle");
            _languageDropdown = query.Q<DropdownField>("language-dropdown");
            _closeButton = query.Q<Button>("back-btn");

            _masterVolumeChangedCallback = OnMasterVolumeChanged;
            _fullscreenChangedCallback = OnFullscreenChanged;
            _languageChangedCallback = OnLanguageChanged;

            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.RegisterValueChangedCallback(_masterVolumeChangedCallback);
            }

            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.RegisterValueChangedCallback(_fullscreenChangedCallback);
            }

            if (_languageDropdown != null)
            {
                _languageDropdown.RegisterValueChangedCallback(_languageChangedCallback);
            }

            if (_closeButton != null)
            {
                _closeButton.clicked += OnClose;
            }

            _settingsService.Changed += OnSettingsChanged;
            _localizationService.Changed += OnLocalizationChanged;
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

            RefreshLocalizedText();
            RefreshLanguageDropdown();
        }

        public void Unbind()
        {
            _settingsService.Changed -= OnSettingsChanged;
            _localizationService.Changed -= OnLocalizationChanged;

            if (_masterVolumeSlider != null && _masterVolumeChangedCallback != null)
            {
                _masterVolumeSlider.UnregisterValueChangedCallback(_masterVolumeChangedCallback);
            }

            if (_fullscreenToggle != null && _fullscreenChangedCallback != null)
            {
                _fullscreenToggle.UnregisterValueChangedCallback(_fullscreenChangedCallback);
            }

            if (_languageDropdown != null && _languageChangedCallback != null)
            {
                _languageDropdown.UnregisterValueChangedCallback(_languageChangedCallback);
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

        private void OnLocalizationChanged()
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

        private void OnLanguageChanged(ChangeEvent<string> evt)
        {
            var selectedCode = ResolveLanguageCode(evt.newValue);
            if (string.IsNullOrWhiteSpace(selectedCode))
            {
                return;
            }

            _localizationService.SetLanguage(selectedCode);
        }

        private void OnClose()
        {
            _navigator.HideOverlay(ScreenId.Settings);
        }

        private void RefreshLocalizedText()
        {
            if (_titleLabel != null)
            {
                _titleLabel.text = _localizationService.GetText("ui.settings.title");
            }

            if (_masterVolumeLabel != null)
            {
                _masterVolumeLabel.text = _localizationService.GetText("ui.settings.masterVolume");
            }

            if (_fullscreenLabel != null)
            {
                _fullscreenLabel.text = _localizationService.GetText("ui.settings.fullscreen");
            }

            if (_languageLabel != null)
            {
                _languageLabel.text = _localizationService.GetText("ui.settings.language");
            }

            if (_closeButton != null)
            {
                _closeButton.text = _localizationService.GetText("ui.settings.close");
            }
        }

        private void RefreshLanguageDropdown()
        {
            if (_languageDropdown == null)
            {
                return;
            }

            _languageCodes.Clear();
            _languageLabels.Clear();

            var options = _localizationService.Languages;
            for (var i = 0; i < options.Count; i++)
            {
                var code = options[i].code;
                if (string.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                var key = $"ui.language.{code}";
                var localizedLabel = _localizationService.GetText(key);
                if (string.Equals(localizedLabel, key))
                {
                    localizedLabel = string.IsNullOrWhiteSpace(options[i].displayName)
                        ? code
                        : options[i].displayName;
                }

                _languageCodes.Add(code);
                _languageLabels.Add(localizedLabel);
            }

            if (_languageCodes.Count == 0)
            {
                _languageCodes.Add("en");
                _languageLabels.Add("English");
            }

            _languageDropdown.choices = new List<string>(_languageLabels);

            var selectedCode = _settingsService.CurrentLanguageCode;
            var selectedIndex = 0;
            for (var i = 0; i < _languageCodes.Count; i++)
            {
                if (string.Equals(_languageCodes[i], selectedCode, System.StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }

            _languageDropdown.SetValueWithoutNotify(_languageLabels[selectedIndex]);
        }

        private string ResolveLanguageCode(string selectedLabel)
        {
            for (var i = 0; i < _languageLabels.Count; i++)
            {
                if (string.Equals(_languageLabels[i], selectedLabel, System.StringComparison.Ordinal))
                {
                    return _languageCodes[i];
                }
            }

            return string.Empty;
        }
    }
}
