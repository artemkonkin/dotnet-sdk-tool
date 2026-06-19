using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using dotnet_sdk_tool_template.Models;
using dotnet_sdk_tool_template.Services;

namespace dotnet_sdk_tool_template.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ReleaseService _releaseService;
    private readonly DotnetInstaller _installer;
    private readonly LegacyFrameworkInstaller _legacyInstaller;
    private readonly Progress<string> _installProgress;

    private List<DotnetChannelViewModel> _allChannels = new();
    private CancellationTokenSource? _installCts;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private string _statusMessage = "Loading .NET releases…";
    [ObservableProperty] private string _installLog = string.Empty;

    [ObservableProperty] private DotnetChannelViewModel? _installingChannel;

    [ObservableProperty] private bool _isInstalling;

    [ObservableProperty] private bool _showLegacy;

    public ObservableCollection<DotnetChannelViewModel> Channels { get; } = new();

    /// <summary>Cumulative .NET Framework 4.x line — only one of these is installable at a time.</summary>
    public ObservableCollection<LegacyFrameworkViewModel> Net4Versions { get; } = new();

    /// <summary>Stand-alone legacy versions (3.5 SP1, 2.0 SP2, 1.1 SP1).</summary>
    public ObservableCollection<LegacyFrameworkViewModel> OtherLegacyVersions { get; } = new();

    public string PlatformDescription => DotnetInstaller.PlatformDescription;
    public string InstallDir => DotnetInstaller.InstallDir;

    /// <summary>Legacy .NET Framework is Windows-only — drives visibility of that section.</summary>
    public bool IsWindows => LegacyFrameworkInstaller.IsSupported;

    public MainWindowViewModel()
        : this(new HttpClient())
    {
    }

    public MainWindowViewModel(HttpClient httpClient)
    {
        _releaseService = new ReleaseService(httpClient);
        _installer = new DotnetInstaller(httpClient);
        _legacyInstaller = new LegacyFrameworkInstaller(httpClient);
        _installProgress = new Progress<string>(AppendLog);

        LoadLegacyFrameworks();
        _ = LoadAsync();
    }

    private void LoadLegacyFrameworks()
    {
        if (!IsWindows)
            return;

        foreach (var fw in LegacyFrameworkInstaller.Load())
        {
            var vm = new LegacyFrameworkViewModel(fw, RequestLegacyInstallAsync);
            if (fw.version.StartsWith("4", StringComparison.Ordinal))
                Net4Versions.Add(vm);
            else
                OtherLegacyVersions.Add(vm);
        }

        // Pre-select the newest 4.x (first in the JSON) so the radio group has a default.
        if (Net4Versions.Count > 0)
            Net4Versions[0].IsSelected = true;
    }

    partial void OnShowLegacyChanged(bool value) => ApplyFilter();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        StatusMessage = "Loading .NET releases…";
        try
        {
            var releases = await _releaseService.GetReleasesAsync();
            var installed = await _installer.GetInstalledSdksAsync();

            _allChannels = releases.Select(r =>
            {
                var vm = new DotnetChannelViewModel(r, _releaseService, RequestInstallAsync);
                vm.IsInstalled = installed.Any(v => v.StartsWith(r.channel_version + ".", StringComparison.Ordinal));
                return vm;
            }).ToList();

            ApplyFilter();
            StatusMessage = $"{_allChannels.Count} channels available · installs to {InstallDir}";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Could not load releases: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var visible = _allChannels.Where(c => ShowLegacy || !c.IsEol);

        Channels.Clear();
        foreach (var channel in visible)
            Channels.Add(channel);
    }

    /// <summary>
    /// Centralised install entry point invoked by channel cards and individual version rows.
    /// Only one install runs at a time. Pass <paramref name="version"/> = null for the channel latest.
    /// </summary>
    public async Task RequestInstallAsync(string channelVersion, string? version, InstallKind kind, IInstallable item)
    {
        if (IsInstalling)
            return;

        var label = string.IsNullOrWhiteSpace(version) ? channelVersion : version;

        _installCts = new CancellationTokenSource();
        InstallingChannel = item as DotnetChannelViewModel;
        IsInstalling = true;
        item.IsInstalling = true;

        AppendLog($"──────── .NET {label} ({kind}) ────────");
        try
        {
            var ok = await _installer.InstallAsync(
                channelVersion, version, kind, _installProgress, _installCts.Token);

            item.IsInstalled = ok || item.IsInstalled;
            StatusMessage = ok
                ? $".NET {label} installed."
                : $".NET {label} installation failed — see log.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = $".NET {label} installation cancelled.";
        }
        catch (Exception ex)
        {
            AppendLog($"Error: {ex.Message}");
            StatusMessage = $"Installation error: {ex.Message}";
        }
        finally
        {
            item.IsInstalling = false;
            InstallingChannel = null;
            IsInstalling = false;
            _installCts.Dispose();
            _installCts = null;
        }
    }

    /// <summary>Installs the currently selected .NET Framework 4.x version.</summary>
    [RelayCommand]
    private async Task InstallNet4()
    {
        var selected = Net4Versions.FirstOrDefault(v => v.IsSelected);
        if (selected is not null)
            await selected.InstallCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Centralised install entry point for legacy .NET Framework versions (offline installer or DISM).
    /// Shares the single-install guard and log with the modern installer.
    /// </summary>
    public async Task RequestLegacyInstallAsync(LegacyFramework framework, IInstallable item)
    {
        if (IsInstalling)
            return;

        _installCts = new CancellationTokenSource();
        IsInstalling = true;
        item.IsInstalling = true;

        AppendLog($"──────── .NET Framework {framework.version} ────────");
        try
        {
            var ok = await _legacyInstaller.InstallAsync(framework, _installProgress, _installCts.Token);

            item.IsInstalled = ok || item.IsInstalled;
            StatusMessage = ok
                ? $".NET Framework {framework.version} installed."
                : $".NET Framework {framework.version} installation failed — see log.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = $".NET Framework {framework.version} installation cancelled.";
        }
        catch (Exception ex)
        {
            AppendLog($"Error: {ex.Message}");
            StatusMessage = $"Installation error: {ex.Message}";
        }
        finally
        {
            item.IsInstalling = false;
            IsInstalling = false;
            _installCts.Dispose();
            _installCts = null;
        }
    }

    [RelayCommand]
    private void Cancel() => _installCts?.Cancel();

    [RelayCommand]
    private void ClearLog() => InstallLog = string.Empty;

    private void AppendLog(string line)
    {
        InstallLog = string.IsNullOrEmpty(InstallLog) ? line : $"{InstallLog}\n{line}";
    }
}
