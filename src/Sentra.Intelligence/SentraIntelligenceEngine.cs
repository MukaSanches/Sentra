using System.Globalization;
using System.Text.RegularExpressions;
using Sentra.Application.Intelligence;

namespace Sentra.Intelligence;

public sealed partial class SentraIntelligenceEngine : IIntelligenceEngine
{
    public IntelligenceDecision Analyze(IntelligenceContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var text = string.Join("\n", context.RecentMessages.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        var normalized = text.ToLowerInvariant();

        if (ContainsAny(normalized, "abrir o portão", "abra o portão", "libera o portão", "acionar portão", "abrir cancela"))
        {
            return new(
                "gate_control",
                "BLOCKED",
                "Solicitação de acionamento de acesso detectada. O SENTRA não executa abertura de portão por inteligência.",
                new Dictionary<string, string?>(),
                Array.Empty<string>(),
                "PROHIBITED",
                null);
        }

        if (ContainsAny(normalized, "encomenda", "pacote", "caixa", "entrega chegou", "correios"))
        {
            return new(
                "package",
                "READY",
                "Conversa relacionada a encomenda. A consulta e a baixa usam somente dados registrados no SENTRA.",
                new Dictionary<string, string?> { ["unit_id"] = context.UnitId?.ToString() },
                Array.Empty<string>(),
                "LOW",
                null);
        }

        if (ContainsAny(normalized, "prestador", "técnico", "tecnico", "manutenção", "manutencao", "encanador", "eletricista"))
        {
            var providerName = ExtractName(text);
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(providerName))
            {
                missing.Add("provider_name");
            }

            return new(
                "provider_authorization",
                missing.Count == 0 ? "READY_FOR_CONFIRMATION" : "NEEDS_CLARIFICATION",
                missing.Count == 0
                    ? $"Autorização de prestador preparada para {providerName}."
                    : "Há intenção de autorizar um prestador, mas o nome ainda não está claro.",
                new Dictionary<string, string?>
                {
                    ["provider_name"] = providerName,
                    ["unit_id"] = context.UnitId?.ToString()
                },
                missing,
                "CONFIRMATION_REQUIRED",
                "create_provider_authorization");
        }

        if (ContainsAny(normalized, "ocorrência", "ocorrencia", "barulho", "elevador", "garagem", "segurança", "seguranca", "problema"))
        {
            return new(
                "occurrence",
                "READY_FOR_CONFIRMATION",
                "Possível ocorrência operacional identificada. O texto original será preservado e o porteiro deve confirmar o registro.",
                new Dictionary<string, string?>
                {
                    ["description"] = text.Length > 2000 ? text[..2000] : text,
                    ["unit_id"] = context.UnitId?.ToString()
                },
                Array.Empty<string>(),
                "CONFIRMATION_REQUIRED",
                "create_occurrence");
        }

        if (ContainsAny(normalized, "visitante", "visita", "vai chegar", "vai aí", "vai ai", "pode liberar", "autoriza", "autorizar"))
        {
            var name = ExtractName(text);
            var plate = ExtractPlate(text);
            var time = ExtractTime(text, context.Now);
            var missing = new List<string>();

            if (string.IsNullOrWhiteSpace(name))
            {
                missing.Add("visitor_name");
            }

            if (context.UnitId is null)
            {
                missing.Add("unit_id");
            }

            if (time is null)
            {
                missing.Add("arrival_time");
            }

            var data = new Dictionary<string, string?>
            {
                ["visitor_name"] = name,
                ["vehicle_plate"] = plate,
                ["unit_id"] = context.UnitId?.ToString(),
                ["resident_id"] = context.ResidentId?.ToString(),
                ["arrival_time"] = time?.ToString("O", CultureInfo.InvariantCulture),
                ["relationship"] = ExtractRelationship(normalized),
                ["purpose"] = ExtractPurpose(normalized)
            };

            return new(
                "visitor_authorization",
                missing.Count == 0 ? "READY_FOR_CONFIRMATION" : "NEEDS_CLARIFICATION",
                missing.Count == 0
                    ? $"Autorização de visitante preparada para {name}. Confirmação humana obrigatória."
                    : $"Intenção de visita detectada. Falta confirmar: {string.Join(", ", missing)}.",
                data,
                missing,
                "CONFIRMATION_REQUIRED",
                "create_visitor_authorization");
        }

        return new(
            "unknown",
            "NEEDS_CLARIFICATION",
            "O SENTRA não encontrou dados suficientes para propor uma ação operacional segura.",
            new Dictionary<string, string?>(),
            ["intent"],
            "NONE",
            null);
    }

    private static bool ContainsAny(string text, params string[] terms)
        => terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static string? ExtractRelationship(string text)
    {
        if (text.Contains("minha mãe") || text.Contains("minha mae")) return "mãe";
        if (text.Contains("meu pai")) return "pai";
        if (text.Contains("meu irmão") || text.Contains("meu irmao")) return "irmão";
        if (text.Contains("minha irmã") || text.Contains("minha irma")) return "irmã";
        if (text.Contains("meu filho")) return "filho";
        if (text.Contains("minha filha")) return "filha";
        if (text.Contains("amigo")) return "amigo";
        if (text.Contains("amiga")) return "amiga";
        return null;
    }

    private static string? ExtractPurpose(string text)
    {
        if (text.Contains("pegar uma caixa") || text.Contains("pegar a caixa") || text.Contains("buscar encomenda")) return "Retirada de encomenda";
        if (text.Contains("manutenção") || text.Contains("manutencao")) return "Manutenção";
        return null;
    }

    private static string? ExtractName(string text)
    {
        var patterns = new[]
        {
            @"(?:meu irmão|minha irmã|minha mãe|minha mae|meu pai|meu filho|minha filha|visitante|nome (?:é|e)|chama(?:-se)?|chamado|chamada)\s+([A-ZÁÀÂÃÉÈÊÍÏÓÔÕÖÚÇ][\p{L}'-]{1,39})",
            @"\b([A-ZÁÀÂÃÉÈÊÍÏÓÔÕÖÚÇ][\p{L}'-]{2,39})\s+vai\s+(?:aí|ai|chegar)"
        };

        foreach (var pattern in patterns)
        {
            foreach (Match match in Regex.Matches(
                         text,
                         pattern,
                         RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                var candidate = NormalizePersonName(match.Groups[1].Value);
                if (candidate is not null)
                {
                    return candidate;
                }
            }
        }

        var lines = text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            var candidate = NormalizeStandaloneName(line);
            if (candidate is not null)
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? NormalizeStandaloneName(string value)
    {
        var candidate = value.Trim().Trim('.', ',', ';', ':', '!', '?');
        if (candidate.Length is < 2 or > 80)
        {
            return null;
        }

        var words = candidate.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (words.Length is < 1 or > 3)
        {
            return null;
        }

        for (var index = 0; index < words.Length; index++)
        {
            var word = words[index];

            if (index > 0 &&
                index < words.Length - 1 &&
                PersonNameParticles.Contains(word))
            {
                continue;
            }

            if (!Regex.IsMatch(
                    word,
                    @"^[A-ZÁÀÂÃÉÈÊÍÏÓÔÕÖÚÇ][\p{L}'-]{1,39}$",
                    RegexOptions.CultureInvariant))
            {
                return null;
            }
        }

        return NormalizePersonName(candidate);
    }

    private static string? NormalizePersonName(string value)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var firstWord = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (firstWord is null || PersonNameStopWords.Contains(firstWord))
        {
            return null;
        }

        return CultureInfo.GetCultureInfo("pt-BR").TextInfo
            .ToTitleCase(normalized.ToLowerInvariant());
    }

    private static readonly HashSet<string> PersonNameParticles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "de", "da", "do", "das", "dos", "e"
        };

    private static readonly HashSet<string> PersonNameStopWords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "vai", "chegar", "chega", "pode", "liberar", "autoriza", "autorizar",
            "visita", "visitante", "carro", "placa", "hoje", "amanhã", "amanha",
            "sim", "não", "nao", "ela", "ele", "minha", "meu", "mãe", "mae",
            "pai", "irmão", "irmao", "irmã", "irma", "filho", "filha"
        };

    private static string? ExtractPlate(string text)
    {
        var match = PlateRegex().Match(text.ToUpperInvariant());
        return match.Success ? match.Value : null;
    }

    private static DateTimeOffset? ExtractTime(string text, DateTimeOffset now)
    {
        var match = TimeRegex().Match(text);
        if (!match.Success)
        {
            return null;
        }

        var hour = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var minute = match.Groups[2].Success && match.Groups[2].Value.Length > 0
            ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)
            : 0;

        var local = new DateTimeOffset(
            now.Year,
            now.Month,
            now.Day,
            hour,
            minute,
            0,
            now.Offset);

        return local < now.AddHours(-2) ? local.AddDays(1) : local;
    }

    [GeneratedRegex(@"\b[A-Z]{3}[0-9][A-Z0-9][0-9]{2}\b", RegexOptions.CultureInvariant)]
    private static partial Regex PlateRegex();

    [GeneratedRegex(@"\b(?:umas?\s*)?([01]?[0-9]|2[0-3])(?:[:hH]([0-5][0-9])?)\b", RegexOptions.CultureInvariant)]
    private static partial Regex TimeRegex();
}
