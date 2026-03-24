using System;
using System.IO;
using Tsukuyomi.Application.Config;
using UnityEngine;

namespace Tsukuyomi.Infrastructure.Config
{
    public sealed class JsonConfigRepository<TConfig> : IConfigRepository<TConfig> where TConfig : class, new()
    {
        private readonly IConfigValidator _validator;
        private readonly string _sourceConfigPath;
        private readonly string _sourceSchemaPath;

        private TConfig _cache;
        private string _rawJson = string.Empty;

        public JsonConfigRepository(
            string configName,
            IConfigValidator validator,
            string sourceConfigPath = null,
            string sourceSchemaPath = null)
        {
            ConfigName = configName ?? throw new ArgumentNullException(nameof(configName));
            _validator = validator;
            _sourceConfigPath = sourceConfigPath;
            _sourceSchemaPath = sourceSchemaPath;
        }

        public string ConfigName { get; }

        public TConfig Get()
        {
            return _cache ?? Reload();
        }

        public TConfig Reload()
        {
            _rawJson = LoadConfigJson();
            var schemaJson = LoadSchemaJson();

            if (!string.IsNullOrWhiteSpace(schemaJson) && _validator != null)
            {
                var validation = _validator.Validate(ConfigName, _rawJson, schemaJson);
                if (!validation.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Config '{ConfigName}' failed schema validation: {validation.ErrorMessage}");
                }
            }

            _cache = JsonUtility.FromJson<TConfig>(_rawJson);
            return _cache ?? new TConfig();
        }

        public string GetRawJson()
        {
            if (string.IsNullOrEmpty(_rawJson))
            {
                _ = Get();
            }

            return _rawJson;
        }

        private string LoadConfigJson()
        {
#if UNITY_EDITOR
            var sourcePath = string.IsNullOrWhiteSpace(_sourceConfigPath)
                ? ConfigPaths.GetConfigSourcePath(ConfigName)
                : _sourceConfigPath;
            if (File.Exists(sourcePath))
            {
                return File.ReadAllText(sourcePath);
            }
#endif
            var textAsset = Resources.Load<TextAsset>($"{ConfigPaths.ConfigResourceDirectory}/{ConfigName}");
            if (textAsset == null)
            {
                throw new FileNotFoundException(
                    $"Missing config text asset in Resources/{ConfigPaths.ConfigResourceDirectory}/{ConfigName}.json");
            }

            return textAsset.text;
        }

        private string LoadSchemaJson()
        {
#if UNITY_EDITOR
            var sourcePath = string.IsNullOrWhiteSpace(_sourceSchemaPath)
                ? ConfigPaths.GetSchemaSourcePath(ConfigName)
                : _sourceSchemaPath;
            if (File.Exists(sourcePath))
            {
                return File.ReadAllText(sourcePath);
            }
#endif
            return string.Empty;
        }
    }
}
