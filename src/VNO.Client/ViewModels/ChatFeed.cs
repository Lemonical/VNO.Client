namespace VNO.Client.ViewModels;

/// <summary>
/// Which text feed the game stage is showing under the stage viewport
/// </summary>
/// <remarks>
/// The legacy client stacked memo_ooc, memo_fi, and memo_debug in the same place
/// and toggled them with the OOC, EVENTS, and ERRORS panel buttons
/// </remarks>
public enum ChatFeed
{
    /// <summary>
    /// The events feed, the default in character side channel
    /// </summary>
    Events,

    /// <summary>
    /// The out of character feed
    /// </summary>
    Ooc,

    /// <summary>
    /// The error and debug feed
    /// </summary>
    Errors
}
