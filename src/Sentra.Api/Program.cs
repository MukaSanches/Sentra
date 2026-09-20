using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Sentra.Api.Endpoints;
using Sentra.Api.Health;
using Sentra.Api.Middleware;
using Sentra.Api.Realtime;
using Sentra.Api.Security;
using Sentra.Application.Abstractions;
using Sentra.Application.Services;
using Sentra.Application.Realtime;
using Sentra.Contracts.System;
using Sentra.Domain.Access;
using Sentra.Infrastructure;
using Sentra.WhatsApp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRealtimeOperationsNotifier, SignalROperationsNotifier>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSentraInfrastructure(builder.Configuration);
builder.Services.AddSentraWhatsApp(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck<SetupReadinessHealthCheck>("setup-readiness", tags: new[] { "ready" });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var authority = builder.Configuration["Authentication:Authority"]
    ?? builder.Configuration["AUTHORITY"];
var audience = builder.Configuration["Authentication:Audience"]
    ?? builder.Configuration["AUTH_AUDIENCE"];

if (!string.IsNullOrWhiteSpace(authority) && !string.IsNullOrWhiteSpace(audience))
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = audience;
            options.RequireHttpsMetadata = true;
        });
}
else
{
    builder.Services.AddAuthentication();
}

builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    foreach (var permissionCode in PermissionCodes.All)
    {
        options.AddPolicy(
            permissionCode,
            policy => policy.Requirements.Add(new PermissionRequirement(permissionCode)));
    }
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", (IHostEnvironment environment) =>
    new SystemStatusResponse(
        "SENTRA — Central Inteligente de Portaria",
        "1.0.0",
        environment.EnvironmentName,
        "m1-operations"));

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.MapHub<OperationsHub>("/hubs/operations");
app.MapOperationalEndpoints();
app.MapWhatsAppEndpoints();

app.Run();

public partial class Program;
