using System;
using System.Collections.Generic;
using System.Linq;

namespace NFC.Platform.BuildingBlocks.Results
{
    /// <summary>
    /// Represents the result of a service operation that returns data.
    /// </summary>
    /// <typeparam name="T">The type of the data returned by the operation.</typeparam>
    public class ServiceResult<T> : ServiceResult
    {
        /// <summary>
        /// Gets the data returned by the service operation.
        /// </summary>
        public T? Data { get; init; }

        protected ServiceResult() { }

        /// <summary>
        /// Creates a successful service result with data.
        /// </summary>
        /// <param name="data">The data returned by the operation.</param>
        /// <param name="message">An optional success message.</param>
        /// <returns>A successful <see cref="ServiceResult{T}"/>.</returns>
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

        /// <summary>
        /// Creates a failed service result with a single error message.
        /// </summary>
        /// <param name="message">The failure message.</param>
        /// <param name="statusCode">The HTTP status code. Defaults to 400 (Bad Request).</param>
        /// <returns>A failed <see cref="ServiceResult{T}"/>.</returns>
        public static new ServiceResult<T> Fail(string? message, int statusCode = 400)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Message = !string.IsNullOrWhiteSpace(message) ? message : "OperationFailed",
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Creates a failed service result with a list of error messages.
        /// </summary>
        /// <param name="errors">The list of validation or execution errors.</param>
        /// <param name="statusCode">The HTTP status code. Defaults to 400 (Bad Request).</param>
        /// <returns>A failed <see cref="ServiceResult{T}"/>.</returns>
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

        /// <summary>
        /// Creates a not found service result.
        /// </summary>
        /// <param name="message">The not found message. Defaults to "RecordNotFound".</param>
        /// <returns>A failed <see cref="ServiceResult{T}"/> with a 404 status code.</returns>
        public static new ServiceResult<T> NotFound(string? message = "RecordNotFound")
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Message = !string.IsNullOrWhiteSpace(message) ? message : "RecordNotFound",
                StatusCode = 404
            };
        }

        /// <summary>
        /// Creates an unauthorized service result.
        /// </summary>
        /// <param name="message">The unauthorized message. Defaults to "UnauthorizedAccess".</param>
        /// <returns>A failed <see cref="ServiceResult{T}"/> with a 401 status code.</returns>
        public static new ServiceResult<T> Unauthorized(string? message = "UnauthorizedAccess")
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Message = !string.IsNullOrWhiteSpace(message) ? message : "UnauthorizedAccess",
                StatusCode = 401
            };
        }

        /// <summary>
        /// Creates a forbidden service result.
        /// </summary>
        /// <param name="message">The forbidden message. Defaults to "ForbiddenAccess".</param>
        /// <returns>A failed <see cref="ServiceResult{T}"/> with a 403 status code.</returns>
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
