using System;

namespace NFC.Platform.BuildingBlocks.Common.Exceptions
{
    public class NotFoundException : Exception
    {
        public string ErrorKey { get; }

        public NotFoundException(string message) : base(message)
        {
            ErrorKey = message;
        }

        public NotFoundException(string message, string errorKey) : base(message)
        {
            ErrorKey = errorKey;
        }
    }
}
