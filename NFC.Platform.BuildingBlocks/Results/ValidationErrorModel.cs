namespace NFC.Platform.BuildingBlocks.Results
{
    public class ValidationErrorModel(string propertyName, string errorMessage)
    {
        public string PropertyName { get; init; } = propertyName;

        public string ErrorMessage { get; init; } = errorMessage;
    }
}
