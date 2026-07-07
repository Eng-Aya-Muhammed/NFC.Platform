using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NFC.Platform.BuildingBlocks.Common.Exceptions;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.API.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IMessageService _messageService;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IMessageService messageService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred during request {Method} {Path}", 
                    context.Request.Method, context.Request.Path);

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            int statusCode = (int)HttpStatusCode.InternalServerError;
            string message;

            switch (exception)
            {
                case NotFoundException notFoundEx:
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = _messageService.Get(notFoundEx.ErrorKey);
                    break;

                case BusinessException businessEx:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = _messageService.Get(businessEx.ErrorKey);
                    break;

                case ForbiddenException forbiddenEx:
                    statusCode = (int)HttpStatusCode.Forbidden;
                    message = _messageService.Get(forbiddenEx.ErrorKey);
                    break;

                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    message = _messageService.Get("Unauthorized");
                    break;

                default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    message = _messageService.Get("UnexpectedError");
                    break;
            }

            context.Response.StatusCode = statusCode;

            var result = ServiceResult.Fail(message, statusCode);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(result, options);
            await context.Response.WriteAsync(json);
        }
    }
}
