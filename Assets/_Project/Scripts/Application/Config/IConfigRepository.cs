namespace Tsukuyomi.Application.Config
{
    public interface IConfigRepository<TConfig> where TConfig : class, new()
    {
        string ConfigName { get; }

        TConfig Get();

        TConfig Reload();

        string GetRawJson();
    }
}
