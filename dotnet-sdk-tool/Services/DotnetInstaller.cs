using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace dotnet_sdk_tool_template.Services;

/// <summary>
/// Which artifact to install for a channel.
/// </summary>
public enum InstallKind
{
    Sdk,
    Runtime,
    AspNetCoreRuntime,
}

/// <summary>
/// Installs .NET versions cross-platform using the official dotnet-install scripts
/// (https://learn.microsoft.com/dotnet/core/tools/dotnet-install-script). Installs into a
/// user-local directory, so no administrator/sudo rights are required, and supports
/// side-by-side versions on Linux, Windows and macOS.
/// </summary>
public class DotnetInstaller
{
    private const string InstallScriptShUrl = "https://dot.net/v1/dotnet-install.sh";
    private const string InstallScriptPs1Url = "https://dot.net/v1/dotnet-install.ps1";

    private readonly HttpClient _httpClient;

    public DotnetInstaller(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// The directory .NET is installed into (matches the dotnet-install default).
    /// </summary>
    public static string InstallDir
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(localAppData, "Microsoft", "dotnet");
            }

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet");
        }
    }

    /// <summary>
    /// Friendly "Windows · x64" style description of the current platform.
    /// </summary>
    public static string PlatformDescription
    {
        get
        {
            string os =
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" :
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" : "Unknown OS";

            return $"{os} · {RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()}";
        }
    }

    /// <summary>
    /// Installs the requested channel, streaming script output line-by-line to <paramref name="output"/>.
    /// Returns true on a zero exit code.
    /// </summary>
    public async Task<bool> InstallAsync(
        string channel,
        InstallKind kind,
        IProgress<string> output,
        CancellationToken cancellationToken)
    {
        var installDir = InstallDir;
        Directory.CreateDirectory(installDir);

        ProcessStartInfo startInfo = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? await BuildWindowsStartInfoAsync(channel, kind, installDir, cancellationToken)
            : await BuildUnixStartInfoAsync(channel, kind, installDir, cancellationToken);

        output.Report($"Installing .NET {channel} ({Describe(kind)}) into {installDir}");
        output.Report($"> {startInfo.FileName} {startInfo.Arguments}");

        return await RunProcessAsync(startInfo, output, cancellationToken);
    }

    /// <summary>
    /// Best-effort list of currently installed SDK versions (via <c>dotnet --list-sdks</c>).
    /// </summary>
    public async Task<IReadOnlyList<string>> GetInstalledSdksAsync(CancellationToken cancellationToken = default)
    {
        var versions = new List<string>();
        try
        {
            var startInfo = new ProcessStartInfo("dotnet", "--list-sdks")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process is null)
                return versions;

            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                // Format: "10.0.301 [/usr/lib/dotnet/sdk]"
                var space = line.IndexOf(' ');
                versions.Add(space > 0 ? line[..space].Trim() : line.Trim());
            }
        }
        catch
        {
            // dotnet not on PATH yet — that's fine, nothing reported as installed.
        }

        return versions;
    }

    private async Task<ProcessStartInfo> BuildUnixStartInfoAsync(
        string channel, InstallKind kind, string installDir, CancellationToken cancellationToken)
    {
        var scriptPath = await DownloadScriptAsync(InstallScriptShUrl, "dotnet-install.sh", cancellationToken);

        var args = new StringBuilder();
        args.Append('"').Append(scriptPath).Append('"');
        args.Append(" --channel ").Append(channel);
        args.Append(" --install-dir \"").Append(installDir).Append('"');
        AppendRuntime(args, kind, dash: "--runtime ");

        return new ProcessStartInfo("bash", args.ToString())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
    }

    private async Task<ProcessStartInfo> BuildWindowsStartInfoAsync(
        string channel, InstallKind kind, string installDir, CancellationToken cancellationToken)
    {
        var scriptPath = await DownloadScriptAsync(InstallScriptPs1Url, "dotnet-install.ps1", cancellationToken);

        var args = new StringBuilder();
        args.Append("-NoProfile -ExecutionPolicy Bypass -File \"").Append(scriptPath).Append('"');
        args.Append(" -Channel ").Append(channel);
        args.Append(" -InstallDir \"").Append(installDir).Append('"');
        AppendRuntime(args, kind, dash: "-Runtime ");

        return new ProcessStartInfo("powershell", args.ToString())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
    }

    private static void AppendRuntime(StringBuilder args, InstallKind kind, string dash)
    {
        switch (kind)
        {
            case InstallKind.Runtime:
                args.Append(' ').Append(dash).Append("dotnet");
                break;
            case InstallKind.AspNetCoreRuntime:
                args.Append(' ').Append(dash).Append("aspnetcore");
                break;
            // Sdk: no --runtime flag (default installs the SDK).
        }
    }

    private static string Describe(InstallKind kind) => kind switch
    {
        InstallKind.Runtime => ".NET Runtime",
        InstallKind.AspNetCoreRuntime => "ASP.NET Core Runtime",
        _ => "SDK",
    };

    private async Task<string> DownloadScriptAsync(string url, string fileName, CancellationToken cancellationToken)
    {
        var dir = Path.Combine(Path.GetTempPath(), "dotnet-sdk-tool");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, fileName);

        var content = await _httpClient.GetStringAsync(url, cancellationToken);
        await File.WriteAllTextAsync(path, content, cancellationToken);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Make the shell script executable (best-effort).
            try
            {
                File.SetUnixFileMode(path,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
            catch
            {
                // Ignored — we invoke via `bash <path>` anyway.
            }
        }

        return path;
    }

    private static async Task<bool> RunProcessAsync(
        ProcessStartInfo startInfo, IProgress<string> output, CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) output.Report(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) output.Report(e.Data);
        };

        if (!process.Start())
        {
            output.Report("Failed to start the install process.");
            return false;
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

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

        if (process.ExitCode == 0)
        {
            output.Report($"Done. Exit code {process.ExitCode}.");
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
