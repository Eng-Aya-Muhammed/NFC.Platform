using System;
using System.Collections.Generic;
using System.Linq;

namespace NFC.Platform.BuildingBlocks.Results
{
    public class ServiceResult
    {
        public bool IsSuccess { get; init; }

        public string? Message { get; init; }

        public int StatusCode { get; init; }

        protected ServiceResult() { }

        public static ServiceResult Success(string? message = null)
        {
            return new ServiceResult
            {
                IsSuccess = true,
                Message = message,
                StatusCode = 200
            };
        }

        public static ServiceResult Fail(string? message, int statusCode = 400)
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Message = !string.IsNullOrWhiteSpace(message) ? message : "OperationFailed",
                StatusCode = statusCode
            };
        }

        public static ServiceResult Fail(List<string>? errors, int statusCode = 400)
        {
            var validErrors = errors?
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim().TrimEnd('.', ' '))
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();

            var message = (validErrors != null && validErrors.Count > 0)
                ? string.Join(", ", validErrors)
                : "OperationFailed";

            return new ServiceResult
            {
                IsSuccess = false,
                Message = message,
                StatusCode = statusCode
            };
        }

        public static ServiceResult NotFound(string? message = "RecordNotFound")
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Message = !string.IsNullOrWhiteSpace(message) ? message : "RecordNotFound",
                StatusCode = 404
            };
        }

        public static ServiceResult Unauthorized(string? message = "UnauthorizedAccess")
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Message = !string.IsNullOrWhiteSpace(message) ? message : "UnauthorizedAccess",
                StatusCode = 401
            };
        }

        public static ServiceResult Forbidden(string? message = "ForbiddenAccess")
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Message = !string.IsNullOrWhiteSpace(message) ? message : "ForbiddenAccess",
                StatusCode = 403
            };
        }
    }
}
