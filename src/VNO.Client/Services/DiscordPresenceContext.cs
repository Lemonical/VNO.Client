namespace VNO.Client.Services;

/// <summary>
/// Candidate game state from which a privacy-safe Discord presence is projected.
/// </summary>
/// <param name="PublicServerName">Server name, only when sourced from the public Master directory.</param>
/// <param name="OnlinePlayers">Current public player count, when known.</param>
/// <param name="PlayerCapacity">Public player capacity, when known.</param>
public sealed record DiscordPresenceContext(
    string? PublicServerName = null,
    int? OnlinePlayers = null,
    int? PlayerCapacity = null);
