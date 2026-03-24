using System;
using System.Collections;
using System.Collections.Generic;
using Tsukuyomi.Application.Config;

namespace Tsukuyomi.Infrastructure.Config
{
    public sealed class JsonSchemaConfigValidator : IConfigValidator
    {
        public ConfigValidationResult Validate(string configName, string json, string schemaJson)
        {
            var configObject = MiniJson.Deserialize(json);
            if (configObject is not Dictionary<string, object> configRoot)
            {
                return ConfigValidationResult.Failure($"{configName}: config json root must be object");
            }

            var schemaObject = MiniJson.Deserialize(schemaJson);
            if (schemaObject is not Dictionary<string, object> schemaRoot)
            {
                return ConfigValidationResult.Failure($"{configName}: schema json root must be object");
            }

            if (ValidateNode("$", configRoot, schemaRoot, out var error))
            {
                return ConfigValidationResult.Success;
            }

            return ConfigValidationResult.Failure($"{configName}: {error}");
        }

        private static bool ValidateNode(string path, object value, Dictionary<string, object> schemaNode, out string error)
        {
            if (schemaNode.TryGetValue("type", out var typeNode) && typeNode is string expectedType)
            {
                if (!ValidateType(expectedType, value))
                {
                    error = $"{path} expected '{expectedType}' but got '{DescribeType(value)}'";
                    return false;
                }
            }

            if (schemaNode.TryGetValue("enum", out var enumNode) && enumNode is IList enumList)
            {
                var matched = false;
                foreach (var enumItem in enumList)
                {
                    if (Equals(enumItem, value))
                    {
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    error = $"{path} value '{value}' is not in enum.";
                    return false;
                }
            }

            if (schemaNode.TryGetValue("type", out typeNode) &&
                typeNode is string typeString &&
                string.Equals(typeString, "object", StringComparison.Ordinal))
            {
                if (!ValidateObject(path, value, schemaNode, out error))
                {
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool ValidateObject(
            string path,
            object value,
            Dictionary<string, object> schemaNode,
            out string error)
        {
            if (value is not Dictionary<string, object> objectValue)
            {
                error = $"{path} expected object.";
                return false;
            }

            if (schemaNode.TryGetValue("required", out var requiredNode) && requiredNode is IList requiredList)
            {
                foreach (var requiredItem in requiredList)
                {
                    if (requiredItem is not string requiredProperty)
                    {
                        continue;
                    }

                    if (!objectValue.ContainsKey(requiredProperty))
                    {
                        error = $"{path}.{requiredProperty} is required.";
                        return false;
                    }
                }
            }

            var additionalPropertiesAllowed = true;
            if (schemaNode.TryGetValue("additionalProperties", out var additionalPropertiesNode) &&
                additionalPropertiesNode is bool allowAdditional)
            {
                additionalPropertiesAllowed = allowAdditional;
            }

            var propertySchemas = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
            if (schemaNode.TryGetValue("properties", out var propertiesNode) &&
                propertiesNode is Dictionary<string, object> propertiesDictionary)
            {
                foreach (var (key, propertySchema) in propertiesDictionary)
                {
                    if (propertySchema is Dictionary<string, object> typedSchema)
                    {
                        propertySchemas[key] = typedSchema;
                    }
                }
            }

            foreach (var (propertyName, propertyValue) in objectValue)
            {
                if (!propertySchemas.TryGetValue(propertyName, out var propertySchema))
                {
                    if (!additionalPropertiesAllowed)
                    {
                        error = $"{path}.{propertyName} is not defined in schema.";
                        return false;
                    }

                    continue;
                }

                if (!ValidateNode($"{path}.{propertyName}", propertyValue, propertySchema, out error))
                {
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool ValidateType(string expectedType, object value)
        {
            return expectedType switch
            {
                "object" => value is Dictionary<string, object>,
                "string" => value is string,
                "number" => value is double or float or int or long,
                "integer" => IsInteger(value),
                "boolean" => value is bool,
                "array" => value is IList,
                _ => true
            };
        }

        private static bool IsInteger(object value)
        {
            return value switch
            {
                int => true,
                long => true,
                double number => Math.Abs(number % 1d) < 0.0000001d,
                float number => Math.Abs(number % 1f) < 0.000001f,
                _ => false
            };
        }

        private static string DescribeType(object value)
        {
            if (value == null)
            {
                return "null";
            }

            return value.GetType().Name;
        }
    }
}
