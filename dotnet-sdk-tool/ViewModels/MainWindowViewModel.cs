using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using dotnet_sdk_tool_template.Services;

namespace dotnet_sdk_tool_template.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ReleaseService _releaseService;
    private readonly DotnetInstaller _installer;
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

    public string PlatformDescription => DotnetInstaller.PlatformDescription;
    public string InstallDir => DotnetInstaller.InstallDir;

    public MainWindowViewModel()
        : this(new HttpClient())
    {
    }

    public MainWindowViewModel(HttpClient httpClient)
    {
        _releaseService = new ReleaseService(httpClient);
        _installer = new DotnetInstaller(httpClient);
        _installProgress = new Progress<string>(AppendLog);

        _ = LoadAsync();
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

    [RelayCommand]
    private void Cancel() => _installCts?.Cancel();

    [RelayCommand]
    private void ClearLog() => InstallLog = string.Empty;

    private void AppendLog(string line)
    {
        InstallLog = string.IsNullOrEmpty(InstallLog) ? line : $"{InstallLog}\n{line}";
    }
}
