namespace VNO.Client.ViewModels;

/// <summary>
/// One row in the server list
/// </summary>
/// <remarks>
/// Mirrors the entries the legacy client showed in listbox_servers, listbox_fav,
/// and listbox_pub. The host and port feed the game server connection
/// </remarks>
public sealed class ServerEntryViewModel
{
    /// <summary>
    /// Display name of the server
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Host name or address of the game server
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// Game server port
    /// </summary>
    public required int Port { get; init; }

    /// <summary>
    /// Free text description shown in memo_serverdesc when selected
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Current player count, or a status word when offline
    /// </summary>
    public string PlayerCount { get; init; } = "Offline";

    /// <summary>
    /// Label shown in the list
    /// </summary>
    public override string ToString() => Name;
}
