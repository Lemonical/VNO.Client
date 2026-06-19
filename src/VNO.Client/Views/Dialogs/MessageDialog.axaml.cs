using Avalonia.Controls;
using Avalonia.Input;

namespace VNO.Client.Views.Dialogs;

/// <summary>
/// Modal message popup, the port of the legacy ShowMessage calls
/// </summary>
public partial class MessageDialog : Window
{
    /// <summary>
    /// Creates the dialog
    /// </summary>
    public MessageDialog()
    {
        InitializeComponent();
        OkButton.Click += (_, _) => Close();
        // the legacy ShowMessage dismissed on Enter or Escape
        KeyDown += (_, e) =>
        {
            if (e.Key is Key.Enter or Key.Escape)
            {
                Close();
            }
        };
    }

    /// <summary>
    /// Sets the message body
    /// </summary>
    public string Message
    {
        set => MessageText.Text = value;
    }
}
