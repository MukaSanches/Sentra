using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Sentra.Api.Realtime;
using Sentra.Application.Abstractions;
using Sentra.Application.Integrations.WhatsApp;
using Sentra.Domain.Conversations;
using Sentra.Domain.Integrations;
using Sentra.Domain.Residents;
using Sentra.Infrastructure.Persistence;

namespace Sentra.Api.Background;

public sealed class WhatsAppWebhookProcessor(
    IServiceScopeFactory scopeFactory,
    IWhatsAppWebhookParser parser,
    IHubContext<OperationsHub> hub,
    ILogger<WhatsAppWebhookProcessor> logger)
    : BackgroundService
{
    private const int MaxAttempts = 5;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processedAny = false;

            for (var index = 0; index < 25; index++)
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db =
                    scope.ServiceProvider.GetRequiredService<SentraDbContext>();
                var clock =
                    scope.ServiceProvider.GetRequiredService<IClock>();

                var webhook = await ClaimNextAsync(
                    db,
                    clock.UtcNow,
                    stoppingToken);

                if (webhook is null)
                {
                    break;
                }

                processedAny = true;

                var webhookId = webhook.Id;
                await using var processingTransaction =
                    await db.Database.BeginTransactionAsync(stoppingToken);

                try
                {
                    var changedCondominiums =
                        await ProcessAsync(
                            db,
                            webhook,
                            clock.UtcNow,
                            stoppingToken);

                    webhook.MarkProcessed(clock.UtcNow);
                    await db.SaveChangesAsync(stoppingToken);
                    await processingTransaction.CommitAsync(stoppingToken);

                    foreach (var condominiumId in changedCondominiums)
                    {
                        await hub.Clients
                            .Group(OperationsHub.GroupName(condominiumId))
                            .SendAsync(
                                "ConversationChanged",
                                new { source = "WhatsApp" },
                                stoppingToken);
                    }
                }
                catch (WhatsAppProcessingException exception)
                {
                    await processingTransaction.RollbackAsync(stoppingToken);
                    await MarkFailedAsync(
                        db,
                        webhookId,
                        exception.Code,
                        clock.UtcNow,
                        stoppingToken);

                    logger.LogWarning(
                        "Webhook {WebhookEventId} falhou com {ErrorCode}; nenhuma alteração operacional parcial foi confirmada.",
                        webhookId,
                        exception.Code);
                }
                catch (Exception exception)
                    when (exception is not OperationCanceledException)
                {
                    await processingTransaction.RollbackAsync(stoppingToken);
                    await MarkFailedAsync(
                        db,
                        webhookId,
                        "processing_failed",
                        clock.UtcNow,
                        stoppingToken);

                    logger.LogError(
                        exception,
                        "Falha ao processar webhook {WebhookEventId}; conteúdo não foi incluído no log e alterações parciais foram revertidas.",
                        webhookId);
                }
            }

            if (!processedAny)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private static async Task MarkFailedAsync(
        SentraDbContext db,
        Guid webhookId,
        string errorCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();

        var webhook = await db.WebhookEvents.SingleAsync(
            item => item.Id == webhookId,
            cancellationToken);

        webhook.MarkFailed(
            errorCode,
            now,
            RetryDelay(webhook.AttemptCount),
            MaxAttempts);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<WebhookEvent?> ClaimNextAsync(
        SentraDbContext db,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var staleBefore = now.AddMinutes(-5);

        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        var webhook = await db.WebhookEvents
            .FromSqlInterpolated($"""
                SELECT *
                FROM webhook_events
                WHERE
                    (
                        status IN ('Received', 'Failed')
                        AND (next_attempt_at IS NULL OR next_attempt_at <= {now})
                    )
                    OR
                    (
                        status = 'Processing'
                        AND updated_at < {staleBefore}
                    )
                ORDER BY received_at
                FOR UPDATE SKIP LOCKED
                LIMIT 1
                """)
            .SingleOrDefaultAsync(cancellationToken);

        if (webhook is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        webhook.MarkProcessing(now);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return webhook;
    }

    private async Task<HashSet<Guid>> ProcessAsync(
        SentraDbContext db,
        WebhookEvent webhook,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        WhatsAppWebhookEnvelope envelope;

        try
        {
            envelope = parser.Parse(webhook.PayloadJson);
        }
        catch (Exception exception)
            when (exception is ArgumentException
                  or InvalidDataException
                  or System.Text.Json.JsonException)
        {
            throw new WhatsAppProcessingException(
                "invalid_payload",
                exception);
        }

        var changedCondominiums = new HashSet<Guid>();

        foreach (var change in envelope.Changes)
        {
            var integration = await db.Integrations.SingleOrDefaultAsync(
                item =>
                    item.Kind == IntegrationKind.WhatsApp &&
                    item.ExternalResourceId == change.PhoneNumberId &&
                    item.Status == IntegrationStatus.Connected,
                cancellationToken);

            if (integration is null)
            {
                throw new WhatsAppProcessingException(
                    "integration_not_registered");
            }

            foreach (var incoming in change.Messages)
            {
                await ProcessIncomingAsync(
                    db,
                    integration,
                    change,
                    incoming,
                    now,
                    cancellationToken);

                changedCondominiums.Add(integration.CondominiumId);
            }

            foreach (var status in change.Statuses)
            {
                await ProcessStatusAsync(
                    db,
                    status,
                    cancellationToken);

                changedCondominiums.Add(integration.CondominiumId);
            }
        }

        return changedCondominiums;
    }

    private static async Task ProcessIncomingAsync(
        SentraDbContext db,
        Integration integration,
        WhatsAppWebhookChange change,
        WhatsAppIncomingMessage incoming,
        DateTimeOffset processingTime,
        CancellationToken cancellationToken)
    {
        if (await db.Messages.AnyAsync(
                item => item.ExternalMessageId == incoming.MessageId,
                cancellationToken))
        {
            return;
        }

        string e164;

        try
        {
            e164 = ResidentPhone.NormalizeE164(
                "+" + incoming.FromWaId.TrimStart('+'));
        }
        catch (ArgumentException exception)
        {
            throw new WhatsAppProcessingException(
                "invalid_sender",
                exception);
        }

        var residentPhone = await db.ResidentPhones.SingleOrDefaultAsync(
            item =>
                item.E164 == e164 &&
                item.WhatsAppEnabled,
            cancellationToken);

        Resident? resident = null;
        Guid? unitId = null;

        if (residentPhone is not null)
        {
            resident = await db.Residents.SingleOrDefaultAsync(
                item =>
                    item.Id == residentPhone.ResidentId &&
                    item.IsActive,
                cancellationToken);

            unitId = await db.ResidentUnits
                .Where(item =>
                    item.ResidentId == residentPhone.ResidentId &&
                    item.EndsAt == null)
                .OrderByDescending(item => item.IsPrimary)
                .Select(item => (Guid?)item.UnitId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var conversation = await db.Conversations.SingleOrDefaultAsync(
            item =>
                item.CondominiumId == integration.CondominiumId &&
                item.Channel == ConversationChannel.WhatsApp &&
                item.ExternalParticipantId == incoming.FromWaId,
            cancellationToken);

        if (conversation is null)
        {
            conversation = new Conversation(
                integration.CondominiumId,
                ConversationChannel.WhatsApp,
                incoming.FromWaId,
                change.ContactName,
                resident?.Id,
                unitId,
                incoming.Timestamp);

            db.Conversations.Add(conversation);
            db.ConversationParticipants.Add(
                new ConversationParticipant(
                    conversation.Id,
                    resident is null
                        ? ConversationParticipantKind.External
                        : ConversationParticipantKind.Resident,
                    incoming.FromWaId,
                    change.ContactName,
                    resident?.Id));
        }
        else if (conversation.ResidentId is null &&
                 resident is not null)
        {
            conversation.LinkResident(
                resident.Id,
                unitId,
                change.ContactName,
                processingTime);
        }

        var message = Message.CreateInbound(
            conversation.Id,
            incoming.MessageId,
            MapKind(incoming.Kind),
            incoming.Text ?? incoming.StructuredDataJson,
            incoming.Timestamp);

        db.Messages.Add(message);

        if (!string.IsNullOrWhiteSpace(incoming.MediaId))
        {
            var attachment = new Attachment(
                message.Id,
                incoming.MediaId,
                incoming.MimeType,
                incoming.FileName,
                incoming.Sha256,
                processingTime);

            attachment.MarkAwaitingConfiguration(processingTime);
            db.Attachments.Add(attachment);
        }

        conversation.RegisterMessage(incoming.Timestamp);
    }

    private static async Task ProcessStatusAsync(
        SentraDbContext db,
        WhatsAppStatusUpdate status,
        CancellationToken cancellationToken)
    {
        var mappedStatus = MapStatus(status.Status);

        var statusExists = await db.MessageStatusEvents.AnyAsync(
            item =>
                item.ExternalMessageId == status.MessageId &&
                item.Status == mappedStatus &&
                item.OccurredAt == status.Timestamp,
            cancellationToken);

        if (!statusExists)
        {
            db.MessageStatusEvents.Add(
                new MessageStatusEvent(
                    status.MessageId,
                    mappedStatus,
                    status.Timestamp,
                    status.RecipientWaId,
                    status.ErrorCode));
        }

        var message = await db.Messages.SingleOrDefaultAsync(
            item => item.ExternalMessageId == status.MessageId,
            cancellationToken);

        message?.ApplyDeliveryStatus(
            mappedStatus,
            status.Timestamp,
            status.ErrorCode);
    }

    private static MessageContentKind MapKind(WhatsAppIncomingKind kind)
        => kind switch
        {
            WhatsAppIncomingKind.Text => MessageContentKind.Text,
            WhatsAppIncomingKind.Image => MessageContentKind.Image,
            WhatsAppIncomingKind.Audio => MessageContentKind.Audio,
            WhatsAppIncomingKind.Video => MessageContentKind.Video,
            WhatsAppIncomingKind.Document => MessageContentKind.Document,
            WhatsAppIncomingKind.Location => MessageContentKind.Location,
            WhatsAppIncomingKind.Contacts => MessageContentKind.Contacts,
            WhatsAppIncomingKind.Interactive => MessageContentKind.Interactive,
            WhatsAppIncomingKind.Reaction => MessageContentKind.Reaction,
            _ => MessageContentKind.Unknown
        };

    private static MessageDeliveryStatus MapStatus(string status)
        => status switch
        {
            "sent" => MessageDeliveryStatus.Sent,
            "delivered" => MessageDeliveryStatus.Delivered,
            "read" => MessageDeliveryStatus.Read,
            "failed" => MessageDeliveryStatus.Failed,
            _ => MessageDeliveryStatus.Unknown
        };

    private static TimeSpan RetryDelay(int attempt)
        => TimeSpan.FromSeconds(
            Math.Min(300, Math.Pow(2, Math.Max(1, attempt)) * 5));

    private sealed class WhatsAppProcessingException : Exception
    {
        public WhatsAppProcessingException(
            string code,
            Exception? innerException = null)
            : base(code, innerException)
        {
            Code = code;
        }

        public string Code { get; }
    }
}
