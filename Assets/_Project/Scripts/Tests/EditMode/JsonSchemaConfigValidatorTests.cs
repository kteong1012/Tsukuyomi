using NUnit.Framework;
using Tsukuyomi.Infrastructure.Config;

namespace Tsukuyomi.Tests.EditMode
{
    public sealed class JsonSchemaConfigValidatorTests
    {
        private const string Schema = @"{
  ""type"": ""object"",
  ""additionalProperties"": false,
  ""required"": [""name"", ""level""],
  ""properties"": {
    ""name"": {""type"": ""string""},
    ""level"": {""type"": ""integer""},
    ""alive"": {""type"": ""boolean""}
  }
}";

        [Test]
        public void Validate_ReturnsSuccess_WhenJsonMatchesSchema()
        {
            var validator = new JsonSchemaConfigValidator();
            const string json = @"{
  ""name"": ""hero"",
  ""level"": 3,
  ""alive"": true
}";

            var result = validator.Validate("unit", json, Schema);
            Assert.That(result.IsValid, Is.True, result.ErrorMessage);
        }

        [Test]
        public void Validate_ReturnsFailure_WhenAdditionalPropertyExists()
        {
            var validator = new JsonSchemaConfigValidator();
            const string json = @"{
  ""name"": ""hero"",
  ""level"": 3,
  ""alive"": true,
  ""extra"": ""forbidden""
}";

            var result = validator.Validate("unit", json, Schema);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("extra"));
        }
    }
}
