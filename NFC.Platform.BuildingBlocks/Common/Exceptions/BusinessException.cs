using System;

namespace NFC.Platform.BuildingBlocks.Common.Exceptions
{
    /// <summary>
    /// Exception thrown when a business rule or validation check fails.
    /// </summary>
    public class BusinessException : Exception
    {
        /// <summary>
        /// Gets the localization key or specific error code associated with this exception.
        /// </summary>
        public string ErrorKey { get; }

        /// <param name="message">The exception message or localization key.</param>
        public BusinessException(string message) : base(message)
        {
            ErrorKey = message;
        }

        /// <param name="message">The exception message.</param>
        /// <param name="errorKey">The localization key for localized API responses.</param>
        public BusinessException(string message, string errorKey) : base(message)
        {
            ErrorKey = errorKey;
        }
    }
}
