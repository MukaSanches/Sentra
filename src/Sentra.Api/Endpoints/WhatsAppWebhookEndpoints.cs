using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Sentra.Application.Abstractions;
using Sentra.Application.Integrations.WhatsApp;
using Sentra.Domain.Integrations;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Api.Endpoints;

public static class WhatsAppWebhookEndpoints
{
    private const long MaxWebhookBytes = 1_048_576;

    public static IEndpointRouteBuilder MapWhatsAppWebhookEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/api/v1/integrations/whatsapp/webhook",
            (
                HttpRequest request,
                IWhatsAppWebhookVerifier verifier) =>
            {
                var mode = request.Query["hub.mode"].ToString();
                var verifyToken =
                    request.Query["hub.verify_token"].ToString();
                var challenge =
                    request.Query["hub.challenge"].ToString();

                if (!verifier.VerifyChallenge(mode, verifyToken) ||
                    string.IsNullOrWhiteSpace(challenge))
                {
                    return Results.StatusCode(
                        StatusCodes.Status403Forbidden);
                }

                return Results.Text(
                    challenge,
                    "text/plain",
                    Encoding.UTF8);
            })
            .AllowAnonymous();

        endpoints.MapPost(
            "/api/v1/integrations/whatsapp/webhook",
            ReceiveAsync)
            .AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> ReceiveAsync(
        HttpRequest request,
        IWhatsAppWebhookVerifier verifier,
        IWhatsAppWebhookParser parser,
        SentraDbContext db,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength is > MaxWebhookBytes)
        {
            return Results.StatusCode(
                StatusCodes.Status413PayloadTooLarge);
        }

        await using var buffer = new MemoryStream();
        await request.Body.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length > MaxWebhookBytes)
        {
            return Results.StatusCode(
                StatusCodes.Status413PayloadTooLarge);
        }

        var payload = buffer.ToArray();
        var signature =
            request.Headers["X-Hub-Signature-256"].ToString();

        if (!verifier.VerifySignature(payload, signature))
        {
            return Results.StatusCode(
                StatusCodes.Status401Unauthorized);
        }

        var json = Encoding.UTF8.GetString(payload);

        try
        {
            _ = parser.Parse(json);
        }
        catch (Exception exception)
            when (exception is ArgumentException
                  or InvalidDataException
                  or System.Text.Json.JsonException)
        {
            return Results.BadRequest(
                new { error = "Payload de webhook inválido." });
        }

        var payloadHash = Convert.ToHexString(
            SHA256.HashData(payload));

        if (await db.WebhookEvents.AnyAsync(
                item =>
                    item.Provider == "MetaWhatsApp" &&
                    item.PayloadHash == payloadHash,
                cancellationToken))
        {
            return Results.Ok(
                new { accepted = true, duplicate = true });
        }

        db.WebhookEvents.Add(
            new WebhookEvent(
                "MetaWhatsApp",
                payloadHash,
                json,
                clock.UtcNow));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (PostgresErrorClassifier.IsUniqueViolation(exception))
        {
            return Results.Ok(
                new { accepted = true, duplicate = true });
        }

        return Results.Ok(
            new { accepted = true, duplicate = false });
    }
}
