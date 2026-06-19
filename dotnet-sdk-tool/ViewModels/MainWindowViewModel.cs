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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInstalling))]
    private DotnetChannelViewModel? _installingChannel;

    [ObservableProperty] private bool _showLegacy;

    public ObservableCollection<DotnetChannelViewModel> Channels { get; } = new();

    public string PlatformDescription => DotnetInstaller.PlatformDescription;
    public string InstallDir => DotnetInstaller.InstallDir;
    public bool IsInstalling => InstallingChannel is not null;

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
                var vm = new DotnetChannelViewModel(r);
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

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallAsync(DotnetChannelViewModel channel)
    {
        if (channel is null || IsInstalling)
            return;

        _installCts = new CancellationTokenSource();
        InstallingChannel = channel;
        channel.IsInstalling = true;
        InstallCommand.NotifyCanExecuteChanged();

        AppendLog($"──────── .NET {channel.ChannelVersion} ────────");
        try
        {
            var ok = await _installer.InstallAsync(
                channel.ChannelVersion, channel.SelectedKind, _installProgress, _installCts.Token);

            channel.IsInstalled = ok || channel.IsInstalled;
            StatusMessage = ok
                ? $".NET {channel.ChannelVersion} installed."
                : $".NET {channel.ChannelVersion} installation failed — see log.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = $".NET {channel.ChannelVersion} installation cancelled.";
        }
        catch (Exception ex)
        {
            AppendLog($"Error: {ex.Message}");
            StatusMessage = $"Installation error: {ex.Message}";
        }
        finally
        {
            channel.IsInstalling = false;
            InstallingChannel = null;
            _installCts.Dispose();
            _installCts = null;
            InstallCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanInstall(DotnetChannelViewModel? channel) => channel is not null && !IsInstalling;

    [RelayCommand]
    private void Cancel() => _installCts?.Cancel();

    [RelayCommand]
    private void ClearLog() => InstallLog = string.Empty;

    private void AppendLog(string line)
    {
        InstallLog = string.IsNullOrEmpty(InstallLog) ? line : $"{InstallLog}\n{line}";
    }
}
