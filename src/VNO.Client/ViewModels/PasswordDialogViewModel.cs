using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VNO.Client.ViewModels;

/// <summary>
/// View model for the password list dialog
/// </summary>
/// <remarks>
/// Ports PasswordDialog which managed a list of passwords with add, remove, and
/// remove all actions. The add and remove buttons enable only when there is text
/// or a selection to act on
/// </remarks>
public sealed partial class PasswordDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    private string _entry = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    private string? _selected;

    /// <summary>
    /// The current password list
    /// </summary>
    public ObservableCollection<string> Passwords { get; } = new();

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private void Add()
    {
        Passwords.Add(Entry);
        Entry = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove()
    {
        if (Selected is not null)
        {
            Passwords.Remove(Selected);
        }
    }

    [RelayCommand]
    private void RemoveAll() => Passwords.Clear();

    private bool CanAdd() => !string.IsNullOrWhiteSpace(Entry);

    private bool CanRemove() => Selected is not null;
}
