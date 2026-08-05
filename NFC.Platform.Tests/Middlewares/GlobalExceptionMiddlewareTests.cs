using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NFC.Platform.BuildingBlocks.Common.Exceptions;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Middlewares;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Middlewares;

public class GlobalExceptionMiddlewareTests
{
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IMessageService _messageService;

    public GlobalExceptionMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<GlobalExceptionMiddleware>>();
        _messageService = Substitute.For<IMessageService>();
    }

    [Fact]
    public async Task InvokeAsync_BusinessExceptionWithArgs_ReturnsBadRequestAndLocalizedMessage()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var businessEx = new BusinessException("PricingNotConfigured", "Plastic");
        RequestDelegate next = (HttpContext ctx) => throw businessEx;

        var middleware = new GlobalExceptionMiddleware(next, _logger, _messageService);

        _messageService.Get("PricingNotConfigured", Arg.Any<object[]>())
            .ReturnsForAnyArgs("Pricing is not configured for Plastic.");

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);

        using var jsonDoc = JsonDocument.Parse(responseBody);
        var msg = jsonDoc.RootElement.GetProperty("message").GetString();
        Assert.Equal("Pricing is not configured for Plastic.", msg);
    }

    [Fact]
    public async Task InvokeAsync_BusinessExceptionWithoutArgs_ReturnsBadRequestAndLocalizedMessage()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var businessEx = new BusinessException("GeneralError");
        RequestDelegate next = (HttpContext ctx) => throw businessEx;

        var middleware = new GlobalExceptionMiddleware(next, _logger, _messageService);

        _messageService.Get("GeneralError").Returns("A general error occurred.");

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);

        using var jsonDoc = JsonDocument.Parse(responseBody);
        var msg = jsonDoc.RootElement.GetProperty("message").GetString();
        Assert.Equal("A general error occurred.", msg);
    }
}
