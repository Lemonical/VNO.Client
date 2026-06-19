using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace VNO.Client.Views.Dialogs;

/// <summary>
/// Modal single value prompt, the port of the legacy InputBox calls
/// </summary>
public partial class InputDialog : Window
{
    /// <summary>
    /// Creates the dialog
    /// </summary>
    public InputDialog()
    {
        InitializeComponent();
        OkButton.Click += OnOk;
        CancelButton.Click += (_, _) => Close(null);
        // the legacy InputBox accepted Enter to confirm and Escape to cancel
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Close(ValueBox.Text ?? string.Empty);
            }
            else if (e.Key == Key.Escape)
            {
                Close(null);
            }
        };
        Opened += (_, _) => ValueBox.Focus();
    }

    private void OnOk(object? sender, RoutedEventArgs e) => Close(ValueBox.Text ?? string.Empty);

    /// <summary>
    /// Sets the prompt above the input box
    /// </summary>
    public string Prompt
    {
        set => PromptText.Text = value;
    }
}
