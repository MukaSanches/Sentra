using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Sentra.Application.System;
using Sentra.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, loggerConfiguration) =>
    loggerConfiguration
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var connectionString = builder.Configuration.GetConnectionString("Sentra");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddSentraInfrastructure(connectionString);
}

var issuer = builder.Configuration["Sentra:Auth:Issuer"];
var audience = builder.Configuration["Sentra:Auth:Audience"];
var signingKey = builder.Configuration["Sentra:Auth:SigningKey"];
var authenticationConfigured =
    !string.IsNullOrWhiteSpace(issuer) &&
    !string.IsNullOrWhiteSpace(audience) &&
    !string.IsNullOrWhiteSpace(signingKey) &&
    signingKey.Length >= 32;

if (authenticationConfigured)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey!)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });
}
else
{
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

app.MapGet("/api/system/status", () =>
    new SystemStatus(
        Product: "SENTRA — Central Inteligente de Portaria",
        Version: "1.0.0-dev",
        DatabaseConfigured: !string.IsNullOrWhiteSpace(connectionString),
        AuthenticationConfigured: authenticationConfigured))
    .AllowAnonymous();

app.Run();

public partial class Program;
