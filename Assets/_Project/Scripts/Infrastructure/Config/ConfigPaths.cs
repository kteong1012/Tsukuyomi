using System.IO;
using UnityEngine;

namespace Tsukuyomi.Infrastructure.Config
{
    public static class ConfigPaths
    {
        public const string ConfigSourceDirectory = "Assets/_Project/Config";
        public const string SchemaSourceDirectory = "Assets/_Project/ConfigSchema";
        public const string ConfigResourceDirectory = "Config";

        public static string GetConfigSourcePath(string configName)
        {
            return Path.Combine(ProjectRootPath, ConfigSourceDirectory, $"{configName}.json");
        }

        public static string GetSchemaSourcePath(string configName)
        {
            return Path.Combine(ProjectRootPath, SchemaSourceDirectory, $"{configName}.schema.json");
        }

        public static string ProjectRootPath =>
            Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
    }
}
