using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using dotnet_sdk_tool_template.Models;

namespace dotnet_sdk_tool_template.Services;

/// <summary>
/// Fetches the official .NET release-metadata index that drives the channel list.
/// </summary>
public class ReleaseService
{
    private const string ReleasesIndexUrl =
        "https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    public ReleaseService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Downloads and parses the releases index, newest channel first.
    /// </summary>
    public async Task<IReadOnlyList<Releases_index>> GetReleasesAsync(CancellationToken cancellationToken = default)
    {
        await using var stream = await _httpClient.GetStreamAsync(ReleasesIndexUrl, cancellationToken);
        var index = await JsonSerializer.DeserializeAsync<DotnetReleasesDot>(stream, JsonOptions, cancellationToken);

        var channels = index?.releases_index ?? Array.Empty<Releases_index>();

        return channels
            .Where(c => !string.IsNullOrWhiteSpace(c.channel_version))
            .OrderByDescending(c => ParseVersion(c.channel_version))
            .ToList();
    }

    /// <summary>
    /// Downloads the per-channel <c>releases.json</c> and returns every release in the
    /// channel, newest version first.
    /// </summary>
    public async Task<IReadOnlyList<Releases>> GetChannelReleasesAsync(
        string releasesJsonUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(releasesJsonUrl))
            return Array.Empty<Releases>();

        await using var stream = await _httpClient.GetStreamAsync(releasesJsonUrl, cancellationToken);
        var metadata = await JsonSerializer.DeserializeAsync<ReleaseMetadataDto>(stream, JsonOptions, cancellationToken);

        var releases = metadata?.releases ?? Array.Empty<Releases>();

        return releases
            .Where(r => !string.IsNullOrWhiteSpace(r.release_version))
            .OrderByDescending(r => ParseVersion(r.release_version))
            .ToList();
    }

    private static Version ParseVersion(string version)
    {
        // Strip any pre-release suffix (e.g. "10.0.0-preview.5") before comparing.
        var core = version.Split('-', 2)[0];
        return Version.TryParse(core, out var parsed) ? parsed : new Version(0, 0);
    }
}
