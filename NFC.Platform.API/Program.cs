using Hangfire;
using NFC.Platform.API.Extensions;
using NFC.Platform.API.Middlewares;
using NFC.Platform.API.Services;
using NFC.Platform.Application.Extensions;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Extensions;
using NFC.Platform.Infrastructure.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAutoMapperConfig();
builder.Services.AddFluentValidationConfig();
builder.Services.AddSwaggerWithJwt();
builder.Services.AddLocalizationConfig();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddRateLimitingConfig();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();

var app = builder.Build();

app.UseCustomMiddlewares();
app.UseCors("DefaultPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAdminAuthorizationFilter() }
});

RecurringJob.AddOrUpdate<ISubscriptionExpiryService>(
    "subscription-expiry-job",
    service => service.ProcessExpiredSubscriptionsAsync(CancellationToken.None),
    Cron.Daily);

app.MapControllers();

await app.MigrateAndSeedDatabaseAsync(app.Environment);

app.Run();
