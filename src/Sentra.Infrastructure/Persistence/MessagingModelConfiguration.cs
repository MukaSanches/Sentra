using Microsoft.EntityFrameworkCore;
using Sentra.Domain.Condominiums;
using Sentra.Domain.Conversations;
using Sentra.Domain.Integrations;
using Sentra.Domain.Residents;

namespace Sentra.Infrastructure.Persistence;

internal static class MessagingModelConfiguration
{
    public static void ConfigureMessagingModel(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Integration>(entity =>
        {
            entity.ToTable("integrations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
            entity.Property(x => x.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.ExternalResourceId).HasColumnName("external_resource_id").HasMaxLength(160).IsRequired();
            entity.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(160);
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.LastVerifiedAt).HasColumnName("last_verified_at");
            entity.Property(x => x.LastErrorCode).HasColumnName("last_error_code").HasMaxLength(96);
            entity.HasIndex(x => new { x.CondominiumId, x.Kind })
                .IsUnique()
                .HasDatabaseName("ux_integrations_condominium_kind");
            entity.HasIndex(x => new { x.Kind, x.ExternalResourceId })
                .IsUnique()
                .HasDatabaseName("ux_integrations_kind_external_resource");
            entity.HasOne<Condominium>()
                .WithMany()
                .HasForeignKey(x => x.CondominiumId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WebhookEvent>(entity =>
        {
            entity.ToTable("webhook_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(64).IsRequired();
            entity.Property(x => x.PayloadHash).HasColumnName("payload_hash").HasMaxLength(128).IsRequired();
            entity.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("text").IsRequired();
            entity.Property(x => x.ReceivedAt).HasColumnName("received_at").IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.AttemptCount).HasColumnName("attempt_count").IsRequired();
            entity.Property(x => x.ProcessedAt).HasColumnName("processed_at");
            entity.Property(x => x.NextAttemptAt).HasColumnName("next_attempt_at");
            entity.Property(x => x.LastErrorCode).HasColumnName("last_error_code").HasMaxLength(96);
            entity.HasIndex(x => new { x.Provider, x.PayloadHash })
                .IsUnique()
                .HasDatabaseName("ux_webhook_events_provider_hash");
            entity.HasIndex(x => new { x.Status, x.NextAttemptAt, x.ReceivedAt })
                .HasDatabaseName("ix_webhook_events_processing");
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("conversations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
            entity.Property(x => x.Channel).HasColumnName("channel").HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.ExternalParticipantId).HasColumnName("external_participant_id").HasMaxLength(160).IsRequired();
            entity.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(160);
            entity.Property(x => x.ResidentId).HasColumnName("resident_id");
            entity.Property(x => x.UnitId).HasColumnName("unit_id");
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.OpenedAt).HasColumnName("opened_at").IsRequired();
            entity.Property(x => x.LastMessageAt).HasColumnName("last_message_at").IsRequired();
            entity.HasIndex(x => new { x.CondominiumId, x.Channel, x.ExternalParticipantId })
                .IsUnique()
                .HasDatabaseName("ux_conversations_channel_participant");
            entity.HasIndex(x => new { x.CondominiumId, x.LastMessageAt })
                .HasDatabaseName("ix_conversations_condominium_last_message");
            entity.HasOne<Condominium>()
                .WithMany()
                .HasForeignKey(x => x.CondominiumId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Resident>()
                .WithMany()
                .HasForeignKey(x => x.ResidentId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Unit>()
                .WithMany()
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.ToTable("conversation_participants");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.ConversationId).HasColumnName("conversation_id").IsRequired();
            entity.Property(x => x.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.ExternalIdentifier).HasColumnName("external_identifier").HasMaxLength(160).IsRequired();
            entity.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(160);
            entity.Property(x => x.ReferenceId).HasColumnName("reference_id");
            entity.HasIndex(x => new { x.ConversationId, x.ExternalIdentifier })
                .IsUnique()
                .HasDatabaseName("ux_conversation_participants_external");
            entity.HasOne<Conversation>()
                .WithMany()
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.ConversationId).HasColumnName("conversation_id").IsRequired();
            entity.Property(x => x.Direction).HasColumnName("direction").HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(x => x.ContentKind).HasColumnName("content_kind").HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.Text).HasColumnName("text").HasMaxLength(4096);
            entity.Property(x => x.ExternalMessageId).HasColumnName("external_message_id").HasMaxLength(256);
            entity.Property(x => x.ClientRequestId).HasColumnName("client_request_id");
            entity.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
            entity.Property(x => x.DeliveryStatus).HasColumnName("delivery_status").HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.DeliveryStatusAt).HasColumnName("delivery_status_at").IsRequired();
            entity.Property(x => x.LastErrorCode).HasColumnName("last_error_code").HasMaxLength(96);
            entity.HasIndex(x => x.ExternalMessageId)
                .IsUnique()
                .HasFilter("\"external_message_id\" IS NOT NULL")
                .HasDatabaseName("ux_messages_external_message_id");
            entity.HasIndex(x => x.ClientRequestId)
                .IsUnique()
                .HasFilter("\"client_request_id\" IS NOT NULL")
                .HasDatabaseName("ux_messages_client_request_id");
            entity.HasIndex(x => new { x.ConversationId, x.OccurredAt })
                .HasDatabaseName("ix_messages_conversation_occurred_at");
            entity.HasOne<Conversation>()
                .WithMany()
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.ToTable("attachments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.MessageId).HasColumnName("message_id").IsRequired();
            entity.Property(x => x.ExternalMediaId).HasColumnName("external_media_id").HasMaxLength(160).IsRequired();
            entity.Property(x => x.MimeType).HasColumnName("mime_type").HasMaxLength(160);
            entity.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(255);
            entity.Property(x => x.Sha256).HasColumnName("sha256").HasMaxLength(128);
            entity.Property(x => x.StorageStatus).HasColumnName("storage_status").HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.StorageKey).HasColumnName("storage_key").HasMaxLength(512);
            entity.HasIndex(x => x.ExternalMediaId)
                .IsUnique()
                .HasDatabaseName("ux_attachments_external_media_id");
            entity.HasOne<Message>()
                .WithMany()
                .HasForeignKey(x => x.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MessageStatusEvent>(entity =>
        {
            entity.ToTable("message_status_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(x => x.ExternalMessageId).HasColumnName("external_message_id").HasMaxLength(256).IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
            entity.Property(x => x.RecipientWaId).HasColumnName("recipient_wa_id").HasMaxLength(32);
            entity.Property(x => x.ErrorCode).HasColumnName("error_code").HasMaxLength(96);
            entity.HasIndex(x => new { x.ExternalMessageId, x.Status, x.OccurredAt })
                .IsUnique()
                .HasDatabaseName("ux_message_status_events_external_status_time");
        });
    }
}
