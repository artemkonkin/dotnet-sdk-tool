using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform;
using dotnet_sdk_tool_template.Models;

namespace dotnet_sdk_tool_template.Services;

/// <summary>
/// Installs legacy Windows-only .NET Framework versions described in
/// Assets/legacy_framework_versions.json. Two strategies are supported:
/// <list type="bullet">
/// <item>offline_installer — download the redistributable and run it (passive, elevated).</item>
/// <item>windows_feature — enable the feature via DISM (used for 3.5 SP1 / NetFx3 on Win10+).</item>
/// </list>
/// All installs require elevation, so the OS shows a UAC prompt.
/// </summary>
public class LegacyFrameworkInstaller
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    public LegacyFrameworkInstaller(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public static bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Loads the bundled legacy framework list. Returns an empty list if the asset is missing.
    /// </summary>
    public static IReadOnlyList<LegacyFramework> Load()
    {
        try
        {
            var assembly = typeof(LegacyFrameworkInstaller).Assembly.GetName().Name;
            var uri = new Uri($"avares://{assembly}/Assets/legacy_framework_versions.json");
            using var stream = AssetLoader.Open(uri);
            var file = JsonSerializer.Deserialize<LegacyFrameworkFile>(stream, JsonOptions);
            return file?.legacy_frameworks ?? Array.Empty<LegacyFramework>();
        }
        catch
        {
            return Array.Empty<LegacyFramework>();
        }
    }

    /// <summary>
    /// Installs the given legacy framework, streaming progress to <paramref name="output"/>.
    /// </summary>
    public async Task<bool> InstallAsync(LegacyFramework framework, IProgress<string> output, CancellationToken cancellationToken)
    {
        if (!IsSupported)
        {
            output.Report(".NET Framework can only be installed on Windows.");
            return false;
        }

        if (string.Equals(framework.install_type, "windows_feature", StringComparison.OrdinalIgnoreCase))
            return await InstallWindowsFeatureAsync(framework, output, cancellationToken);

        return await InstallOfflineInstallerAsync(framework, output, cancellationToken);
    }

    private async Task<bool> InstallWindowsFeatureAsync(
        LegacyFramework framework, IProgress<string> output, CancellationToken cancellationToken)
    {
        var feature = string.IsNullOrWhiteSpace(framework.dism_name) ? "NetFx3" : framework.dism_name;
        var arguments = $"/online /enable-feature /featurename:{feature} /all /norestart";

        output.Report($"Enabling Windows feature {feature} (this also activates .NET 2.0/3.0).");
        output.Report($"> dism {arguments}");

        return await RunElevatedAsync("dism.exe", arguments, output, cancellationToken);
    }

    private async Task<bool> InstallOfflineInstallerAsync(
        LegacyFramework framework, IProgress<string> output, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(framework.download_url))
        {
            output.Report("No download URL for this version.");
            return false;
        }

        output.Report($"Downloading .NET Framework {framework.version} …");
        var installerPath = await DownloadInstallerAsync(framework, cancellationToken);
        output.Report($"Saved to {installerPath}");

        // /passive shows a progress UI without prompting; /norestart leaves rebooting to the user.
        const string arguments = "/passive /norestart";
        output.Report($"> {installerPath} {arguments}");

        return await RunElevatedAsync(installerPath, arguments, output, cancellationToken);
    }

    private async Task<string> DownloadInstallerAsync(LegacyFramework framework, CancellationToken cancellationToken)
    {
        var dir = Path.Combine(Path.GetTempPath(), "dotnet-sdk-tool", "framework");
        Directory.CreateDirectory(dir);

        var safeVersion = framework.version.Replace(' ', '_').Replace('.', '_');
        var path = Path.Combine(dir, $"NetFx_{safeVersion}.exe");

        using var response = await _httpClient.GetAsync(
            framework.download_url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = File.Create(path);
        await source.CopyToAsync(target, cancellationToken);

        return path;
    }

    /// <summary>
    /// Runs a process elevated (UAC). Output cannot be redirected when elevating, so only the
    /// exit code is reported.
    /// </summary>
    private static async Task<bool> RunElevatedAsync(
        string fileName, string arguments, IProgress<string> output, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = true,
            Verb = "runas",
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        try
        {
            if (!process.Start())
            {
                output.Report("Failed to start the installer.");
                return false;
            }
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            // User declined the UAC prompt, or elevation is unavailable.
            output.Report($"Elevation was cancelled or failed: {ex.Message}");
            return false;
        }

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            output.Report("Installation cancelled.");
            throw;
        }

        // 0 = success, 3010 = success but a reboot is required.
        if (process.ExitCode is 0 or 3010)
        {
            output.Report(process.ExitCode == 3010
                ? "Done. A restart is required to finish."
                : $"Done. Exit code {process.ExitCode}.");
            return true;
        }

        output.Report($"Failed. Exit code {process.ExitCode}.");
        return false;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best-effort.
        }
    }
}
