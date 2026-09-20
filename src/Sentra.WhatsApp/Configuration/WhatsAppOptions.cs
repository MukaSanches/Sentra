namespace Sentra.WhatsApp.Configuration;

public sealed class WhatsAppOptions
{
    public string GraphApiVersion { get; init; } = string.Empty;
    public string AppId { get; init; } = string.Empty;
    public string WabaId { get; init; } = string.Empty;
    public string PhoneNumberId { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string VerifyToken { get; init; } = string.Empty;
    public string AppSecret { get; init; } = string.Empty;
    public string PublicBaseUrl { get; init; } = string.Empty;

    public IReadOnlyList<string> Missing()
    {
        var missing = new List<string>();
        AddIfMissing(GraphApiVersion, "META_GRAPH_API_VERSION", missing);
        AddIfMissing(AppId, "META_APP_ID", missing);
        AddIfMissing(WabaId, "META_WABA_ID", missing);
        AddIfMissing(PhoneNumberId, "META_PHONE_NUMBER_ID", missing);
        AddIfMissing(AccessToken, "META_ACCESS_TOKEN", missing);
        AddIfMissing(VerifyToken, "META_VERIFY_TOKEN", missing);
        AddIfMissing(AppSecret, "META_APP_SECRET", missing);
        AddIfMissing(PublicBaseUrl, "PUBLIC_BASE_URL", missing);
        return missing;
    }

    public bool IsComplete => Missing().Count == 0;

    private static void AddIfMissing(string value, string name, ICollection<string> missing)
    {
        if (string.IsNullOrWhiteSpace(value)) missing.Add(name);
    }
}
