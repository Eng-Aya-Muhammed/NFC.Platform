namespace NFC.Platform.BuildingBlocks.Results
{
    /// <summary>
    /// Represents a model validation error message for a specific property.
    /// </summary>
    public class ValidationErrorModel(string propertyName, string errorMessage)
    {
        /// <summary>
        /// Gets the name of the property that failed validation.
        /// </summary>
        public string PropertyName { get; init; } = propertyName;

        /// <summary>
        /// Gets the validation error message.
        /// </summary>
        public string ErrorMessage { get; init; } = errorMessage;
    }
}
