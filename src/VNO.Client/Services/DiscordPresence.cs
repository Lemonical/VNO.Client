namespace VNO.Client.Services;

/// <summary>
/// Sanitised, display-only Discord activity fields.
/// </summary>
/// <param name="Details">Primary activity description.</param>
/// <param name="State">Optional low-risk public activity state.</param>
public sealed record DiscordPresence(string Details, string? State);
