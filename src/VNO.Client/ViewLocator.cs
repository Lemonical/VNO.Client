using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using VNO.Client.ViewModels;

namespace VNO.Client;

/// <summary>
/// Resolves a view for a view model by naming convention
/// </summary>
/// <remarks>
/// A view model named SomethingViewModel maps to a view named SomethingView in
/// the Views namespace. This lets a ContentControl bound to a view model show the
/// right control without a hand written template per type
/// </remarks>
public sealed class ViewLocator : IDataTemplate
{
    /// <inheritdoc />
    public Control Build(object? data)
    {
        if (data is null)
        {
            return new TextBlock { Text = "No view model" };
        }

        var name = data.GetType().FullName!
            .Replace("ViewModel", "View", StringComparison.Ordinal)
            .Replace(".ViewModels.", ".Views.", StringComparison.Ordinal);

        var type = Type.GetType(name);
        if (type is null)
        {
            return new TextBlock { Text = $"View not found: {name}" };
        }

        return (Control)Activator.CreateInstance(type)!;
    }

    /// <inheritdoc />
    public bool Match(object? data) => data is ViewModelBase;
}
