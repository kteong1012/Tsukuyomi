using System;

namespace Tsukuyomi.Application.Config
{
    public interface IConfigHotReloadService : IDisposable
    {
        event Action<string> Reloaded;

        void Start();

        void Stop();
    }
}
