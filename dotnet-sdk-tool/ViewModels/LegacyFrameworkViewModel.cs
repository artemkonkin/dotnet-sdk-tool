using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using dotnet_sdk_tool_template.Models;

namespace dotnet_sdk_tool_template.ViewModels;

/// <summary>
/// One legacy .NET Framework version. Used both for stand-alone cards (3.5 SP1, 2.0 SP2, 1.1 SP1)
/// and for the radio-selectable 4.x line.
/// </summary>
public partial class LegacyFrameworkViewModel : ViewModelBase, IInstallable
{
    private readonly LegacyFramework _model;
    private readonly Func<LegacyFramework, IInstallable, Task> _install;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusyOrInstalled))]
    private bool _isInstalled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusyOrInstalled))]
    private bool _isInstalling;

    /// <summary>Selection state for the 4.x radio group.</summary>
    [ObservableProperty] private bool _isSelected;

    public LegacyFrameworkViewModel(LegacyFramework model, Func<LegacyFramework, IInstallable, Task> install)
    {
        _model = model;
        _install = install;
    }

    public string Version => _model.version;
    public string ReleaseDate => _model.release_date;
    public string Title => $".NET Framework {Version}";
    public string RadioLabel => $".NET Framework {Version}  ·  {ReleaseDate}";

    public bool IsWindowsFeature =>
        string.Equals(_model.install_type, "windows_feature", StringComparison.OrdinalIgnoreCase);

    public string InstallTypeText => IsWindowsFeature
        ? "Windows feature (DISM) · also enables .NET 2.0/3.0"
        : $"Offline installer · {ReleaseDate}";

    public bool IsBusyOrInstalled => IsInstalling || IsInstalled;

    [RelayCommand]
    private Task Install() => _install(_model, this);
}
