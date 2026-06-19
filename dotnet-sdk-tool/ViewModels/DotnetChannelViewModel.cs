using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using dotnet_sdk_tool_template.Models;
using dotnet_sdk_tool_template.Services;

namespace dotnet_sdk_tool_template.ViewModels;

/// <summary>
/// A package kind paired with a human-readable label for the kind selector.
/// </summary>
public record InstallKindOption(InstallKind Kind, string Label);

/// <summary>
/// One .NET release channel (e.g. "10.0") presented as an installable card. Expanding the card
/// lazily loads every concrete version in the channel via <see cref="ReleaseService"/>.
/// </summary>
public partial class DotnetChannelViewModel : ViewModelBase, IInstallable
{
    private readonly Releases_index _model;
    private readonly ReleaseService _releaseService;
    private readonly InstallRequest _install;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusyOrInstalled))]
    private bool _isInstalled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusyOrInstalled))]
    private bool _isInstalling;

    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private bool _isLoadingReleases;
    [ObservableProperty] private string? _releasesError;

    private bool _releasesLoaded;

    /// <summary>Package kinds offered by the channel-level install button.</summary>
    public IReadOnlyList<InstallKindOption> AvailableKinds { get; }

    [ObservableProperty] private InstallKindOption _selectedKind;

    /// <summary>Concrete versions in this channel (populated on first expand).</summary>
    public ObservableCollection<DotnetReleaseViewModel> Releases { get; } = new();

    public DotnetChannelViewModel(Releases_index model, ReleaseService releaseService, InstallRequest install)
    {
        _model = model;
        _releaseService = releaseService;
        _install = install;

        var kinds = new List<InstallKindOption>
        {
            new(InstallKind.Sdk, "SDK"),
            new(InstallKind.Runtime, ".NET Runtime"),
            new(InstallKind.AspNetCoreRuntime, "ASP.NET Core Runtime"),
        };
        if (DotnetInstaller.SupportsWindowsDesktop)
            kinds.Add(new(InstallKind.WindowsDesktopRuntime, "Windows Desktop Runtime"));

        AvailableKinds = kinds;
        _selectedKind = kinds[0];
    }

    public string ChannelVersion => _model.channel_version;
    public string LatestSdk => _model.latest_sdk;
    public string LatestRuntime => _model.latest_runtime;
    public string LatestReleaseDate => _model.latest_release_date;
    public bool IsSecurity => _model.security;

    /// <summary>LTS / STS pill text.</summary>
    public string ReleaseType => (_model.release_type ?? string.Empty).ToUpperInvariant();

    public string SupportPhase => _model.support_phase ?? string.Empty;

    public bool IsEol => string.Equals(SupportPhase, "eol", System.StringComparison.OrdinalIgnoreCase);

    public bool IsPreview =>
        string.Equals(SupportPhase, "preview", System.StringComparison.OrdinalIgnoreCase) ||
        string.Equals(SupportPhase, "go-live", System.StringComparison.OrdinalIgnoreCase);

    /// <summary>Stable key used by converters to pick the badge colour.</summary>
    public string StatusKey => SupportPhase.ToLowerInvariant() switch
    {
        "active" => "active",
        "maintenance" => "maintenance",
        "preview" or "go-live" => "preview",
        "eol" => "eol",
        _ => "unknown",
    };

    public string StatusText => SupportPhase.ToLowerInvariant() switch
    {
        "active" => "Active",
        "maintenance" => "Maintenance",
        "preview" => "Preview",
        "go-live" => "Go-Live",
        "eol" => "End of life",
        _ => SupportPhase,
    };

    public string EolText => string.IsNullOrWhiteSpace(_model.eol_date)
        ? "No end-of-life date announced"
        : $"End of support: {_model.eol_date}";

    public bool IsBusyOrInstalled => IsInstalling || IsInstalled;

    /// <summary>Installs the latest version of the channel using the selected kind.</summary>
    [RelayCommand]
    private Task InstallLatest() => _install(ChannelVersion, null, SelectedKind.Kind, this);

    /// <summary>Loads the version list the first time the card is expanded.</summary>
    partial void OnIsExpandedChanged(bool value)
    {
        if (value && !_releasesLoaded && !IsLoadingReleases)
            _ = LoadReleasesAsync();
    }

    private async Task LoadReleasesAsync()
    {
        IsLoadingReleases = true;
        ReleasesError = null;
        try
        {
            var releases = await _releaseService.GetChannelReleasesAsync(_model.releases_json);
            Releases.Clear();
            foreach (var release in releases)
                Releases.Add(new DotnetReleaseViewModel(release, ChannelVersion, _install));

            _releasesLoaded = true;
        }
        catch (System.Exception ex)
        {
            ReleasesError = $"Could not load versions: {ex.Message}";
        }
        finally
        {
            IsLoadingReleases = false;
        }
    }
}
