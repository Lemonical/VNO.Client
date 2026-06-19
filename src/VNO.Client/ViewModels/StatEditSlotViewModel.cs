using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VNO.Client.Services;
using VNO.Core.Protocol;

namespace VNO.Client.ViewModels;

/// <summary>
/// One target slot on the animator HP and MP edit tab
/// </summary>
/// <remarks>
/// Form7 laid out eight identical slots, each with a character id, an HP value, an
/// MP value, an Edit Value button that adds the values, and a Set Max button. This
/// models one of those slots so the tab is a simple list of eight
/// </remarks>
public sealed partial class StatEditSlotViewModel : ObservableObject
{
    private const string HealthStat = "HP";
    private const string ManaStat = "MP";
    private const string MaxHealthStat = "MAXHP";
    private const string MaxManaStat = "MAXMP";

    private readonly IServerConnection _server;

    [ObservableProperty]
    private string _charId = "Char ID of target";

    [ObservableProperty]
    private string _health = "HP";

    [ObservableProperty]
    private string _mana = "MP";

    /// <summary>
    /// Creates a slot over the server link
    /// </summary>
    public StatEditSlotViewModel(IServerConnection server) => _server = server;

    [RelayCommand]
    private async Task EditValueAsync()
    {
        if (string.IsNullOrWhiteSpace(CharId))
        {
            return;
        }

        await _server.SendAsync(new NetworkMessage(MessageType.StatChange, CharId, HealthStat, Health)).ConfigureAwait(false);
        await _server.SendAsync(new NetworkMessage(MessageType.StatChange, CharId, ManaStat, Mana)).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SetMaxAsync()
    {
        if (string.IsNullOrWhiteSpace(CharId))
        {
            return;
        }

        await _server.SendAsync(new NetworkMessage(MessageType.StatChange, CharId, MaxHealthStat, Health)).ConfigureAwait(false);
        await _server.SendAsync(new NetworkMessage(MessageType.StatChange, CharId, MaxManaStat, Mana)).ConfigureAwait(false);
    }

    /// <summary>
    /// Tries to read the health field as an integer for callers that need it
    /// </summary>
    public bool TryGetHealth(out int value) =>
        int.TryParse(Health, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}
