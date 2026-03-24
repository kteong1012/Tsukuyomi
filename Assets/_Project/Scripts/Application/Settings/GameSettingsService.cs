using System;
using Tsukuyomi.Application.Config;
using Tsukuyomi.Generated.Config;
using UnityEngine;

namespace Tsukuyomi.Application.Settings
{
    public sealed class GameSettingsService : IGameSettingsService
    {
        private const string MasterVolumeKey = "audio.masterVolume";
        private const string FullscreenKey = "video.fullscreen";

        private readonly IConfigRepository<GameSettingsConfig> _repository;
        private readonly IConfigHotReloadService _hotReloadService;
        private readonly ISettingsStore _settingsStore;

        private GameSettingsConfig _data;

        public GameSettingsService(
            IConfigRepository<GameSettingsConfig> repository,
            IConfigHotReloadService hotReloadService,
            ISettingsStore settingsStore)
        {
            _repository = repository;
            _hotReloadService = hotReloadService;
            _settingsStore = settingsStore;

            _hotReloadService.Reloaded += OnConfigReloaded;
            _hotReloadService.Start();
            Reload();
        }

        public event Action Changed;

        public GameSettingsConfig Data => _data;

        public void Reload()
        {
            var latest = Clone(_repository.Reload());
            ApplyUserOverrides(latest);
            _data = latest;
            ApplyRuntimeEffects();
            Changed?.Invoke();
        }

        public void SetMasterVolume(float value)
        {
            if (_data == null)
            {
                return;
            }

            _data.audio.masterVolume = Mathf.Clamp01(value);
            _settingsStore.SetFloat(MasterVolumeKey, _data.audio.masterVolume);
            _settingsStore.Save();
            ApplyRuntimeEffects();
            Changed?.Invoke();
        }

        public void SetFullscreen(bool enabled)
        {
            if (_data == null)
            {
                return;
            }

            _data.video.fullscreen = enabled;
            _settingsStore.SetBool(FullscreenKey, enabled);
            _settingsStore.Save();
            ApplyRuntimeEffects();
            Changed?.Invoke();
        }

        private void OnConfigReloaded(string configName)
        {
            if (!string.Equals(configName, _repository.ConfigName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Reload();
        }

        private void ApplyUserOverrides(GameSettingsConfig config)
        {
            if (_settingsStore.TryGetFloat(MasterVolumeKey, out var masterVolume))
            {
                config.audio.masterVolume = Mathf.Clamp01(masterVolume);
            }

            if (_settingsStore.TryGetBool(FullscreenKey, out var fullscreen))
            {
                config.video.fullscreen = fullscreen;
            }
        }

        private void ApplyRuntimeEffects()
        {
            AudioListener.volume = _data?.audio?.masterVolume ?? 1f;
            Screen.fullScreen = _data?.video?.fullscreen ?? true;
            UnityEngine.Application.targetFrameRate = _data?.video?.targetFrameRate ?? 60;
        }

        private static GameSettingsConfig Clone(GameSettingsConfig source)
        {
            var json = JsonUtility.ToJson(source);
            return JsonUtility.FromJson<GameSettingsConfig>(json);
        }
    }
}
