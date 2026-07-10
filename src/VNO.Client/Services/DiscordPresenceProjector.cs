using System;
using System.Text;

namespace VNO.Client.Services;

/// <summary>
/// Applies the player's privacy choice to candidate Rich Presence data.
/// </summary>
public static class DiscordPresenceProjector
{
    private const int MaximumTextLength = 128;
    private const string RunningDetails = "Playing Visual Novel Online";

    /// <summary>
    /// Creates a safe display projection, or null when presence is disabled.
    /// </summary>
    public static DiscordPresence? Project(DiscordPresenceMode mode, DiscordPresenceContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (mode == DiscordPresenceMode.Off)
        {
            return null;
        }

        if (mode == DiscordPresenceMode.Running)
        {
            return new DiscordPresence(RunningDetails, null);
        }

        var serverName = Sanitise(context.PublicServerName);
        if (serverName.Length == 0)
        {
            return new DiscordPresence(RunningDetails, null);
        }

        var state = $"Server: {serverName}";
        if (mode == DiscordPresenceMode.PublicServerAndPlayerCount &&
            context.OnlinePlayers is >= 0 &&
            context.PlayerCapacity is > 0 &&
            context.OnlinePlayers <= context.PlayerCapacity)
        {
            state += $" · Players: {context.OnlinePlayers}/{context.PlayerCapacity}";
        }

        return new DiscordPresence(RunningDetails, Truncate(state));
    }

    private static string Sanitise(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var result = new StringBuilder(value.Length);
        var pendingSpace = false;
        foreach (var character in value.Trim())
        {
            if (char.IsControl(character) || char.IsWhiteSpace(character))
            {
                pendingSpace = result.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                result.Append(' ');
                pendingSpace = false;
            }
            result.Append(character);
        }

        return Truncate(result.ToString());
    }

    private static string Truncate(string value) =>
        value.Length <= MaximumTextLength ? value : value[..MaximumTextLength];
}
