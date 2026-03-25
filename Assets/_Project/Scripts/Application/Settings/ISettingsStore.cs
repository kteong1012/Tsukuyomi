namespace Tsukuyomi.Application.Settings
{
    public interface ISettingsStore
    {
        bool TryGetFloat(string key, out float value);

        bool TryGetBool(string key, out bool value);

        bool TryGetString(string key, out string value);

        void SetFloat(string key, float value);

        void SetBool(string key, bool value);

        void SetString(string key, string value);

        void Save();
    }
}
