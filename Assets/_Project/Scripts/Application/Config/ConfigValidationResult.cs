namespace Tsukuyomi.Application.Config
{
    public readonly struct ConfigValidationResult
    {
        public ConfigValidationResult(bool isValid, string errorMessage)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        public bool IsValid { get; }

        public string ErrorMessage { get; }

        public static ConfigValidationResult Success => new(true, string.Empty);

        public static ConfigValidationResult Failure(string message) => new(false, message);
    }
}
