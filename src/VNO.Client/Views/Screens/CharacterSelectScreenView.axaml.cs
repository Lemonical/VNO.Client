using Avalonia.Controls;

namespace VNO.Client.Views;

/// <summary>
/// Code behind for the character select screen
/// </summary>
/// <remarks>
/// Lives in the Views namespace so the view locator can resolve it from
/// CharacterSelectScreenViewModel by convention
/// </remarks>
public sealed partial class CharacterSelectScreenView : UserControl
{
    /// <summary>
    /// Builds the screen and loads its XAML
    /// </summary>
    public CharacterSelectScreenView() => InitializeComponent();
}
