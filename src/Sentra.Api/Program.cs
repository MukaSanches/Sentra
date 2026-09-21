using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Sentra.Api.Background;
using Sentra.Api.Endpoints;
using Sentra.Api.Health;
using Sentra.Api.Middleware;
using Sentra.Api.Realtime;
using Sentra.Application.Abstractions;
using Sentra.Application.Security;
using Sentra.Application.Services;
using Sentra.Contracts.System;
using Sentra.Infrastructure;
using Sentra.WhatsApp;
using Sentra.Intelligence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSentraInfrastructure(builder.Configuration);
builder.Services.AddSentraWhatsApp(builder.Configuration);
builder.Services.AddSentraIntelligence();
builder.Services.AddHealthChecks()
    .AddCheck<SetupReadinessHealthCheck>(
        "setup-readiness",
        tags: new[] { "ready" });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey:
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
});

var authMode = builder.Configuration["Authentication:Mode"]
    ?? builder.Configuration["AUTH_MODE"];
var authConfigured = false;
var localAuth = false;

if (string.Equals(authMode, "Local", StringComparison.OrdinalIgnoreCase))
{
    var issuer = builder.Configuration["Authentication:Issuer"]
        ?? builder.Configuration["AUTH_ISSUER"];
    var audience = builder.Configuration["Authentication:Audience"]
        ?? builder.Configuration["AUTH_AUDIENCE"];
    var signingKey = builder.Configuration["Authentication:SigningKey"]
        ?? builder.Configuration["AUTH_SIGNING_KEY"];

    if (!string.IsNullOrWhiteSpace(issuer) &&
        !string.IsNullOrWhiteSpace(audience) &&
        !string.IsNullOrWhiteSpace(signingKey) &&
        signingKey.Length >= 32)
    {
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(signingKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
            });

        authConfigured = true;
        localAuth = true;
    }
}
else if (string.Equals(
             authMode,
             "External",
             StringComparison.OrdinalIgnoreCase))
{
    var authority = builder.Configuration["Authentication:Authority"]
        ?? builder.Configuration["AUTHORITY"];
    var audience = builder.Configuration["Authentication:Audience"]
        ?? builder.Configuration["AUTH_AUDIENCE"];

    if (!string.IsNullOrWhiteSpace(authority) &&
        !string.IsNullOrWhiteSpace(audience))
    {
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = true;
            });

        authConfigured = true;
    }
}

if (!authConfigured)
{
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in PermissionCatalog.All)
    {
        options.AddPolicy(
            permission.Code,
            policy => policy.RequireClaim("permission", permission.Code));
    }
});

var postgres = builder.Configuration.GetConnectionString("Postgres")
    ?? builder.Configuration["DATABASE_CONNECTION_STRING"];
var databaseConfigured = !string.IsNullOrWhiteSpace(postgres);

if (databaseConfigured)
{
    builder.Services.AddHostedService<WhatsAppWebhookProcessor>();
}

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet(
    "/",
    (IHostEnvironment environment) =>
        new SystemStatusResponse(
            "SENTRA — Central Inteligente de Portaria",
            "1.0.0",
            environment.EnvironmentName,
            "v1-completion"));

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions { Predicate = _ => false });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready")
    });

app.MapHub<OperationsHub>("/hubs/operations");

if (databaseConfigured)
{
    app.MapSentraSetupEndpoints();
    app.MapWhatsAppWebhookEndpoints();

    if (localAuth)
    {
        app.MapSentraAuthEndpoints();
    }

    if (authConfigured)
    {
        app.MapSentraCoreOperationsEndpoints();
        app.MapWhatsAppIntegrationEndpoints();
        app.MapConversationEndpoints();
        app.MapSentraIntelligenceEndpoints();
        app.MapSentraPortariaEndpoints();
    }
}

app.Run();

public partial class Program;
