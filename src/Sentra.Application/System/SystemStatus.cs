namespace Sentra.Application.System;

public sealed record SystemStatus(
    string Product,
    string Version,
    bool DatabaseConfigured,
    bool AuthenticationConfigured);
