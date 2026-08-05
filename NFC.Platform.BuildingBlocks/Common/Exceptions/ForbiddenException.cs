using System;

namespace NFC.Platform.BuildingBlocks.Common.Exceptions
{
    public class ForbiddenException : Exception
    {
        public string ErrorKey { get; }

        public ForbiddenException(string message) : base(message)
        {
            ErrorKey = message;
        }

        public ForbiddenException(string message, string errorKey) : base(message)
        {
            ErrorKey = errorKey;
        }
    }
}
