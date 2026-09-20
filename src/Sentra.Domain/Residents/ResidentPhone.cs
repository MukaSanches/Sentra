using Sentra.Domain.Common;

namespace Sentra.Domain.Residents;

public sealed class ResidentPhone : EntityBase
{
    private ResidentPhone() { }

    public ResidentPhone(Guid residentId, string e164, bool isPrimary = true, bool whatsappEnabled = true)
    {
        if (residentId == Guid.Empty) throw new ArgumentException("Morador inválido.", nameof(residentId));

        ResidentId = residentId;
        E164 = NormalizeE164(e164);
        IsPrimary = isPrimary;
        WhatsAppEnabled = whatsappEnabled;
    }

    public Guid ResidentId { get; private set; }
    public string E164 { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; }
    public bool WhatsAppEnabled { get; private set; }

    public static string NormalizeE164(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Telefone é obrigatório.", nameof(value));
        }

        var normalized = value.Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("(", string.Empty, StringComparison.Ordinal)
            .Replace(")", string.Empty, StringComparison.Ordinal);

        if (!normalized.StartsWith('+') ||
            normalized.Length is < 9 or > 16 ||
            normalized[1..].Any(character => !char.IsDigit(character)))
        {
            throw new ArgumentException("Telefone deve estar no formato E.164, por exemplo +5511999999999.", nameof(value));
        }

        return normalized;
    }
}
