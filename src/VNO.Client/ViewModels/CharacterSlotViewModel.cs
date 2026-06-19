using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VNO.Client.ViewModels;

/// <summary>
/// One selectable character on the character select screen
/// </summary>
/// <remarks>
/// The legacy client laid characters out in a paged grid where taken characters
/// showed their on image and available characters the off image. This holds the
/// per slot state and the two roster images for that grid
/// </remarks>
public sealed partial class CharacterSlotViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CellImage))]
    private bool _isTaken;

    /// <summary>
    /// Character display name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Page this character appears on, one based
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>Grid cell image for the available state</summary>
    public Bitmap? OffImage { get; init; }

    /// <summary>Grid cell image for the taken state</summary>
    public Bitmap? OnImage { get; init; }

    /// <summary>Large preview art for this character</summary>
    public Bitmap? BigArt { get; init; }

    /// <summary>
    /// The image the grid cell shows, on when taken like the legacy roster
    /// </summary>
    public Bitmap? CellImage => IsTaken ? OnImage ?? OffImage : OffImage;
}
