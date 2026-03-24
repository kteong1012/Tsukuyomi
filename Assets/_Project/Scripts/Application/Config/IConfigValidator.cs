namespace Tsukuyomi.Application.Config
{
    public interface IConfigValidator
    {
        ConfigValidationResult Validate(string configName, string json, string schemaJson);
    }
}
