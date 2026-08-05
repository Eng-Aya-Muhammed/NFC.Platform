using System;
using System.Collections.Generic;
using System.Linq;

namespace NFC.Platform.BuildingBlocks.Results
{
    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; init; }

        protected ServiceResult() { }

        public static ServiceResult<T> Success(T data, string? message = null)
        {
            return new ServiceResult<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message,
                StatusCode = 200
            };
        }

        public static new ServiceResult<T> Fail(string? message, int statusCode = 400)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Message = !string.IsNullOrWhiteSpace(message) ? message : "OperationFailed",
                StatusCode = statusCode
            };
        }

        public static new ServiceResult<T> Fail(List<string>? errors, int statusCode = 400)
        {
            var validErrors = errors?
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim().TrimEnd('.', ' '))
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();

            var message = (validErrors != null && validErrors.Count > 0)
                ? string.Join(", ", validErrors)
                : "OperationFailed";

            return new ServiceResult<T>
            {
                IsSuccess = false,
                Message = message,
                StatusCode = statusCode
            };
        }

        public static new ServiceResult<T> NotFound(string? message = "RecordNotFound")
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Message = !string.IsNullOrWhiteSpace(message) ? message : "RecordNotFound",
                StatusCode = 404
            };
        }

        public static new ServiceResult<T> Unauthorized(string? message = "UnauthorizedAccess")
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Message = !string.IsNullOrWhiteSpace(message) ? message : "UnauthorizedAccess",
                StatusCode = 401
            };
        }

        public static new ServiceResult<T> Forbidden(string? message = "ForbiddenAccess")
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Message = !string.IsNullOrWhiteSpace(message) ? message : "ForbiddenAccess",
                StatusCode = 403
            };
        }
    }
}
