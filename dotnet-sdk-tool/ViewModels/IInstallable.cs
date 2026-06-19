using dotnet_sdk_tool_template.Services;

namespace dotnet_sdk_tool_template.ViewModels;

/// <summary>
/// Anything that can show install progress in the UI (a channel card or a single version row).
/// </summary>
public interface IInstallable
{
    bool IsInstalling { get; set; }
    bool IsInstalled { get; set; }
}

/// <summary>
/// Delegate child view-models use to ask <see cref="MainWindowViewModel"/> to run an install,
/// keeping all install orchestration in one place.
/// </summary>
/// <param name="channelVersion">Channel the artifact belongs to (e.g. "9.0").</param>
/// <param name="version">Exact version (e.g. "9.0.0"), or null to install the channel's latest.</param>
/// <param name="kind">Which artifact to install.</param>
/// <param name="item">The UI element whose install state should reflect progress.</param>
public delegate System.Threading.Tasks.Task InstallRequest(
    string channelVersion, string? version, InstallKind kind, IInstallable item);
