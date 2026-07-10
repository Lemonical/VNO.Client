using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using VNO.Client.ViewModels;

namespace VNO.Client.Views;

/// <summary>
/// Code behind for the main client window
/// </summary>
public sealed partial class MainWindow : Window
{
    private double _aspectRatio;
    private Size _previousClientSize;
    private ClientNavigator? _navigator;

    /// <summary>
    /// Builds the window and loads its XAML
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Resized += OnResized;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_navigator is not null)
        {
            _navigator.PropertyChanged -= OnNavigatorPropertyChanged;
        }

        _navigator = (DataContext as MainWindowViewModel)?.Navigator;
        if (_navigator is not null)
        {
            _navigator.PropertyChanged += OnNavigatorPropertyChanged;
        }
    }

    private void OnNavigatorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ClientNavigator.CurrentScreen))
        {
            return;
        }

        Dispatcher.UIThread.Post(() => SizeToContent = SizeToContent.WidthAndHeight);
    }

    private void OnResized(object? sender, WindowResizedEventArgs e)
    {
        if (e.ClientSize.Width <= 0 || e.ClientSize.Height <= 0)
        {
            return;
        }

        if (e.Reason != WindowResizeReason.User || WindowState != WindowState.Normal)
        {
            _previousClientSize = e.ClientSize;
            if (SizeToContent != SizeToContent.Manual)
            {
                _aspectRatio = e.ClientSize.AspectRatio;
            }

            return;
        }

        if (_aspectRatio <= 0 || _previousClientSize.Width <= 0 || _previousClientSize.Height <= 0)
        {
            _aspectRatio = e.ClientSize.AspectRatio;
            _previousClientSize = e.ClientSize;
            return;
        }

        var constrainedSize = AspectRatioSizeCalculator.Constrain(
            e.ClientSize,
            _previousClientSize,
            _aspectRatio);

        _previousClientSize = constrainedSize;
        if (constrainedSize != e.ClientSize)
        {
            ClientSize = constrainedSize;
        }
    }
}
