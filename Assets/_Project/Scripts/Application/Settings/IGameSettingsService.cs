using System;
using Tsukuyomi.Generated.Config;

namespace Tsukuyomi.Application.Settings
{
    public interface IGameSettingsService
    {
        event Action Changed;

        GameSettingsConfig Data { get; }

        void Reload();

        void SetMasterVolume(float value);

        void SetFullscreen(bool enabled);
    }
}
