using Avalonia.Controls;
using Avalonia.Input;
using VNO.Client.ViewModels;
using VNO.Core.Models;

namespace VNO.Client.Views;

/// <summary>
/// Code behind for the game stage screen
/// </summary>
/// <remarks>
/// Lives in the Views namespace so the view locator can resolve it from
/// GameStageViewModel by convention
/// </remarks>
public sealed partial class GameStageView : UserControl
{
    /// <summary>
    /// Builds the screen and loads its XAML
    /// </summary>
    public GameStageView() => InitializeComponent();

    private void OnMusicDoubleTapped(object? sender, TappedEventArgs e)
    {
        // route the double tapped track to the play command on the view model
        if (sender is ListBox { SelectedItem: MusicTrack track } &&
            DataContext is GameStageViewModel vm &&
            vm.PlayMusicCommand.CanExecute(track))
        {
            vm.PlayMusicCommand.Execute(track);
        }
    }

    private void OnAreaDoubleTapped(object? sender, TappedEventArgs e)
    {
        // route the double tapped area to the join command on the view model
        if (sender is ListBox { SelectedItem: Area area } &&
            DataContext is GameStageViewModel vm &&
            vm.JoinAreaCommand.CanExecute(area))
        {
            vm.JoinAreaCommand.Execute(area);
        }
    }

    private void OnBroadcastImagePressed(object? sender, PointerPressedEventArgs e)
    {
        // clicking the streamed image dismisses it
        if (DataContext is GameStageViewModel vm && vm.ClearBroadcastImageCommand.CanExecute(null))
        {
            vm.ClearBroadcastImageCommand.Execute(null);
        }
    }
}
