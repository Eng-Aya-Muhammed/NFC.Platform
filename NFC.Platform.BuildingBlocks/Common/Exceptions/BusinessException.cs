using System;

namespace NFC.Platform.BuildingBlocks.Common.Exceptions
{
    public class BusinessException : Exception
    {
        public string ErrorKey { get; }

        public object[] Args { get; }

        public BusinessException(string message) : base(message)
        {
            ErrorKey = message;
            Args = [];
        }

        public BusinessException(string message, string errorKey) : base(message)
        {
            ErrorKey = errorKey;
            Args = [];
        }

        public BusinessException(string errorKey, params object[] args) : base(errorKey)
        {
            ErrorKey = errorKey;
            Args = args;
        }
    }
}
