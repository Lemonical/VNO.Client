using CommunityToolkit.Mvvm.ComponentModel;

namespace VNO.Client.ViewModels;

/// <summary>
/// Base class for every view model in the client app
/// </summary>
/// <remarks>
/// Inherits property change support from the Community Toolkit so derived types
/// can use the ObservableProperty and RelayCommand source generators
/// </remarks>
public abstract class ViewModelBase : ObservableObject
{
}
