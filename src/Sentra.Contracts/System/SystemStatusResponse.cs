namespace Sentra.Contracts.System;

public sealed record SystemStatusResponse(
    string Product,
    string Version,
    string Environment,
    string Status);
