using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using dotnet_sdk_tool_template.Models;
using dotnet_sdk_tool_template.Services;

namespace dotnet_sdk_tool_template.ViewModels;

/// <summary>
/// A single concrete .NET release (e.g. "9.0.0") that can be installed as any of its
/// available package kinds (SDK, runtime, ASP.NET Core runtime, Windows Desktop runtime).
/// </summary>
public partial class DotnetReleaseViewModel : ViewModelBase, IInstallable
{
    private readonly Releases _model;
    private readonly string _channelVersion;
    private readonly InstallRequest _install;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusyOrInstalled))]
    private bool _isInstalled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusyOrInstalled))]
    private bool _isInstalling;

    public DotnetReleaseViewModel(Releases model, string channelVersion, InstallRequest install)
    {
        _model = model;
        _channelVersion = channelVersion;
        _install = install;
    }

    public string ReleaseVersion => _model.release_version;
    public string ReleaseDate => _model.release_date;
    public bool IsSecurity => _model.security;

    public string SdkVersion => _model.sdk?.version;
    public string RuntimeVersion => _model.runtime?.version;
    public string AspNetVersion => _model.aspnetcore_runtime?.version;
    public string WindowsDesktopVersion => _model.windowsdesktop?.version;

    public bool HasSdk => !string.IsNullOrWhiteSpace(SdkVersion);
    public bool HasRuntime => !string.IsNullOrWhiteSpace(RuntimeVersion);
    public bool HasAspNet => !string.IsNullOrWhiteSpace(AspNetVersion);
    public bool HasWindowsDesktop =>
        !string.IsNullOrWhiteSpace(WindowsDesktopVersion) && DotnetInstaller.SupportsWindowsDesktop;

    public string SdkButtonText => $"SDK {SdkVersion}";
    public string RuntimeButtonText => $"Runtime {RuntimeVersion}";
    public string AspNetButtonText => $"ASP.NET Core {AspNetVersion}";
    public string WindowsDesktopButtonText => $"Windows Desktop {WindowsDesktopVersion}";

    public string CveText
    {
        get
        {
            var count = _model.cve_list?.Length ?? 0;
            return count == 0 ? string.Empty : $"🛡 {count} CVE fix{(count == 1 ? "" : "es")}";
        }
    }

    public bool IsBusyOrInstalled => IsInstalling || IsInstalled;

    [RelayCommand]
    private Task InstallKind(InstallKind kind)
    {
        var version = kind switch
        {
            Services.InstallKind.Sdk => SdkVersion,
            Services.InstallKind.Runtime => RuntimeVersion,
            Services.InstallKind.AspNetCoreRuntime => AspNetVersion,
            Services.InstallKind.WindowsDesktopRuntime => WindowsDesktopVersion,
            _ => null,
        };

        return _install(_channelVersion, version, kind, this);
    }
}
