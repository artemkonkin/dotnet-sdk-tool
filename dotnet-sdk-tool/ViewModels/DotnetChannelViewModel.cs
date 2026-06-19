using CommunityToolkit.Mvvm.ComponentModel;
using dotnet_sdk_tool_template.Models;
using dotnet_sdk_tool_template.Services;

namespace dotnet_sdk_tool_template.ViewModels;

/// <summary>
/// One .NET release channel (e.g. "10.0") presented as an installable card.
/// </summary>
public partial class DotnetChannelViewModel : ViewModelBase
{
    private readonly Releases_index _model;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusyOrInstalled))]
    private bool _isInstalled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusyOrInstalled))]
    private bool _isInstalling;

    [ObservableProperty] private bool _runtimeOnly;

    public DotnetChannelViewModel(Releases_index model)
    {
        _model = model;
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

    /// <summary>The artifact to install based on the per-card toggle.</summary>
    public InstallKind SelectedKind => RuntimeOnly ? InstallKind.Runtime : InstallKind.Sdk;
}
