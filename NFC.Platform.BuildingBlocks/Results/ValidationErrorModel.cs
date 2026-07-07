namespace NFC.Platform.BuildingBlocks.Results
{
    /// <summary>
    /// Represents a model validation error message for a specific property.
    /// </summary>
    public class ValidationErrorModel
    {
        /// <summary>
        /// Gets the name of the property that failed validation.
        /// </summary>
        public string PropertyName { get; init; }

        /// <summary>
        /// Gets the validation error message.
        /// </summary>
        public string ErrorMessage { get; init; }

        public ValidationErrorModel(string propertyName, string errorMessage)
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
        }
    }
}
