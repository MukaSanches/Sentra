using Microsoft.EntityFrameworkCore;
using Sentra.Domain.Properties;
using Sentra.Domain.Residents;
using Sentra.Domain.WhatsApp;

namespace Sentra.Infrastructure.Persistence;

internal static class WhatsAppModelConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WhatsAppIntegration>(entity =>
        {
            entity.ToTable("whatsapp_integrations");
            entity.HasKey(x => x.Id);
            Base(entity);
            entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
            entity.Property(x => x.WabaId).HasColumnName("waba_id").HasMaxLength(80).IsRequired();
            entity.Property(x => x.PhoneNumberId).HasColumnName("phone_number_id").HasMaxLength(80).IsRequired();
            entity.Property(x => x.DisplayPhoneNumber).HasColumnName("display_phone_number").HasMaxLength(60);
            entity.Property(x => x.VerifiedName).HasColumnName("verified_name").HasMaxLength(160);
            entity.Property(x => x.QualityRating).HasColumnName("quality_rating").HasMaxLength(30);
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled").IsRequired();
            entity.Property(x => x.IsAppSubscribed).HasColumnName("is_app_subscribed").IsRequired();
            entity.Property(x => x.LastValidatedAt).HasColumnName("last_validated_at");
            entity.Property(x => x.LastWebhookAt).HasColumnName("last_webhook_at");
            entity.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(160).IsRequired();
            entity.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(160).IsRequired();
            entity.HasIndex(x => x.CondominiumId).IsUnique().HasDatabaseName("ux_whatsapp_integrations_condominium");
            entity.HasIndex(x => x.PhoneNumberId).IsUnique().HasDatabaseName("ux_whatsapp_integrations_phone");
            entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WhatsAppConversation>(entity =>
        {
            entity.ToTable("whatsapp_conversations");
            entity.HasKey(x => x.Id);
            Base(entity);
            entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
            entity.Property(x => x.ExternalParticipantId).HasColumnName("external_participant_id").HasMaxLength(32).IsRequired();
            entity.Property(x => x.ResidentId).HasColumnName("resident_id");
            entity.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(160);
            entity.Property(x => x.LastInboundAt).HasColumnName("last_inbound_at");
            entity.Property(x => x.LastMessageAt).HasColumnName("last_message_at").IsRequired();
            entity.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(160).IsRequired();
            entity.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(160).IsRequired();
            entity.HasIndex(x => new { x.CondominiumId, x.ExternalParticipantId }).IsUnique()
                .HasDatabaseName("ux_whatsapp_conversations_participant");
            entity.HasIndex(x => new { x.CondominiumId, x.LastMessageAt })
                .HasDatabaseName("ix_whatsapp_conversations_recent");
            entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Resident>().WithMany().HasForeignKey(x => x.ResidentId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WhatsAppMessage>(entity =>
        {
            entity.ToTable("whatsapp_messages");
            entity.HasKey(x => x.Id);
            Base(entity);
            entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
            entity.Property(x => x.ConversationId).HasColumnName("conversation_id").IsRequired();
            entity.Property(x => x.ExternalMessageId).HasColumnName("external_message_id").HasMaxLength(256);
            entity.Property(x => x.Direction).HasColumnName("direction").HasConversion<int>().IsRequired();
            entity.Property(x => x.Type).HasColumnName("message_type").HasConversion<int>().IsRequired();
            entity.Property(x => x.Text).HasColumnName("text");
            entity.Property(x => x.ReplyToExternalMessageId).HasColumnName("reply_to_external_message_id").HasMaxLength(256);
            entity.Property(x => x.RawPayloadJson).HasColumnName("raw_payload_json").HasColumnType("jsonb");
            entity.Property(x => x.MessageTimestamp).HasColumnName("message_timestamp").IsRequired();
            entity.Property(x => x.DeliveryStatus).HasColumnName("delivery_status").HasConversion<int>().IsRequired();
            entity.Property(x => x.DeliveryStatusAt).HasColumnName("delivery_status_at").IsRequired();
            entity.Property(x => x.ErrorCode).HasColumnName("error_code").HasMaxLength(80);
            entity.Property(x => x.ErrorTitle).HasColumnName("error_title").HasMaxLength(500);
            entity.HasIndex(x => x.ExternalMessageId).IsUnique()
                .HasFilter(""" "external_message_id" IS NOT NULL """)
                .HasDatabaseName("ux_whatsapp_messages_external");
            entity.HasIndex(x => new { x.ConversationId, x.MessageTimestamp })
                .HasDatabaseName("ix_whatsapp_messages_conversation_time");
            entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<WhatsAppConversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WhatsAppAttachment>(entity =>
        {
            entity.ToTable("whatsapp_attachments");
            entity.HasKey(x => x.Id);
            Base(entity);
            entity.Property(x => x.CondominiumId).HasColumnName("condominium_id").IsRequired();
            entity.Property(x => x.MessageId).HasColumnName("message_id").IsRequired();
            entity.Property(x => x.MediaId).HasColumnName("media_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.MimeType).HasColumnName("mime_type").HasMaxLength(160).IsRequired();
            entity.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(255);
            entity.Property(x => x.Sha256).HasColumnName("sha256").HasMaxLength(128);
            entity.Property(x => x.FileSize).HasColumnName("file_size");
            entity.Property(x => x.Content).HasColumnName("content").HasColumnType("bytea");
            entity.Property(x => x.DownloadedAt).HasColumnName("downloaded_at");
            entity.HasIndex(x => x.MessageId).HasDatabaseName("ix_whatsapp_attachments_message");
            entity.HasIndex(x => x.MediaId).HasDatabaseName("ix_whatsapp_attachments_media");
            entity.HasOne<Condominium>().WithMany().HasForeignKey(x => x.CondominiumId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<WhatsAppMessage>().WithMany().HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WhatsAppWebhookEvent>(entity =>
        {
            entity.ToTable("whatsapp_webhook_events");
            entity.HasKey(x => x.Id);
            Base(entity);
            entity.Property(x => x.EventHash).HasColumnName("event_hash").HasMaxLength(64).IsRequired();
            entity.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.ReceivedAt).HasColumnName("received_at").IsRequired();
            entity.Property(x => x.ProcessingStatus).HasColumnName("processing_status").HasConversion<int>().IsRequired();
            entity.Property(x => x.Attempts).HasColumnName("attempts").IsRequired();
            entity.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(2000);
            entity.Property(x => x.ProcessedAt).HasColumnName("processed_at");
            entity.Property(x => x.NextAttemptAt).HasColumnName("next_attempt_at");
            entity.HasIndex(x => x.EventHash).IsUnique().HasDatabaseName("ux_whatsapp_webhook_events_hash");
            entity.HasIndex(x => new { x.ProcessingStatus, x.NextAttemptAt, x.ReceivedAt })
                .HasDatabaseName("ix_whatsapp_webhook_events_queue");
        });
    }

    private static void Base<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : Sentra.Domain.Common.EntityBase
    {
        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
    }
}
