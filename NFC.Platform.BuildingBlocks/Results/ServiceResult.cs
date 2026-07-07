using System;
using System.Collections.Generic;

namespace NFC.Platform.BuildingBlocks.Results
{
    /// <summary>
    /// Represents the result of a service operation that does not return data.
    /// </summary>
    public class ServiceResult
    {
        /// <summary>
        /// Gets a value indicating whether the service operation was successful.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Gets the message description of the result.
        /// </summary>
        public string? Message { get; init; }

        /// <summary>
        /// Gets the list of error messages if the operation failed.
        /// </summary>
        public List<string> Errors { get; init; } = new();

        /// <summary>
        /// Gets the HTTP status code representing the result outcome.
        /// </summary>
        public int StatusCode { get; init; }

        protected ServiceResult() { }

        /// <summary>
        /// Creates a successful service result.
        /// </summary>
        /// <param name="message">An optional success message.</param>
        /// <returns>A successful <see cref="ServiceResult"/>.</returns>
        public static ServiceResult Success(string? message = null)
        {
            return new ServiceResult
            {
                IsSuccess = true,
                Message = message,
                StatusCode = 200
            };
        }

        /// <summary>
        /// Creates a failed service result with a single error message.
        /// </summary>
        /// <param name="message">The failure message.</param>
        /// <param name="statusCode">The HTTP status code. Defaults to 400 (Bad Request).</param>
        /// <returns>A failed <see cref="ServiceResult"/>.</returns>
        public static ServiceResult Fail(string message, int statusCode = 400)
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Message = message,
                Errors = new List<string> { message },
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Creates a failed service result with a list of error messages.
        /// </summary>
        /// <param name="errors">The list of validation or execution errors.</param>
        /// <param name="statusCode">The HTTP status code. Defaults to 400 (Bad Request).</param>
        /// <returns>A failed <see cref="ServiceResult"/>.</returns>
        public static ServiceResult Fail(List<string> errors, int statusCode = 400)
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Errors = errors ?? new List<string>(),
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Creates a not found service result.
        /// </summary>
        /// <param name="message">The not found message. Defaults to "Resource not found".</param>
        /// <returns>A failed <see cref="ServiceResult"/> with a 404 status code.</returns>
        public static ServiceResult NotFound(string message = "Resource not found")
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Message = message,
                Errors = new List<string> { message },
                StatusCode = 404
            };
        }

        /// <summary>
        /// Creates an unauthorized service result.
        /// </summary>
        /// <param name="message">The unauthorized message. Defaults to "Unauthorized access".</param>
        /// <returns>A failed <see cref="ServiceResult"/> with a 401 status code.</returns>
        public static ServiceResult Unauthorized(string message = "Unauthorized access")
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Message = message,
                Errors = new List<string> { message },
                StatusCode = 401
            };
        }

        /// <summary>
        /// Creates a forbidden service result.
        /// </summary>
        /// <param name="message">The forbidden message. Defaults to "Forbidden access".</param>
        /// <returns>A failed <see cref="ServiceResult"/> with a 403 status code.</returns>
        public static ServiceResult Forbidden(string message = "Forbidden access")
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Message = message,
                Errors = new List<string> { message },
                StatusCode = 403
            };
        }
    }
}
