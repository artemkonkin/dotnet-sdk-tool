using System.Text.Json.Serialization;

namespace dotnet_sdk_tool_template.Models;

/// <summary>
/// https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json
/// </summary>
public class DotnetReleasesDot
{
    [JsonPropertyName("releases-index")]
    public Releases_index[] releases_index { get; set; }

    [JsonPropertyName("signature")]
    public Signature signature { get; set; }
}

public class Releases_index
{
    [JsonPropertyName("channel-version")]
    public string channel_version { get; set; }

    [JsonPropertyName("latest-release")]
    public string latest_release { get; set; }

    [JsonPropertyName("latest-release-date")]
    public string latest_release_date { get; set; }

    [JsonPropertyName("security")]
    public bool security { get; set; }

    [JsonPropertyName("latest-runtime")]
    public string latest_runtime { get; set; }

    [JsonPropertyName("latest-sdk")]
    public string latest_sdk { get; set; }

    [JsonPropertyName("product")]
    public string product { get; set; }

    [JsonPropertyName("support-phase")]
    public string support_phase { get; set; }

    [JsonPropertyName("release-type")]
    public string release_type { get; set; }

    [JsonPropertyName("releases.json")]
    public string releases_json { get; set; }

    [JsonPropertyName("supported-os.json")]
    public string supported_os_json { get; set; }

    [JsonPropertyName("eol-date")]
    public string eol_date { get; set; }
}

public class Signature
{
    public string expiration { get; set; }
    public string file { get; set; }
}

/// <summary>
/// https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json
/// </summary>
public class ReleaseMetadataDto
{
    [JsonPropertyName("channel-version")]
    public string channel_version { get; set; }

    [JsonPropertyName("latest-release")]
    public string latest_release { get; set; }

    [JsonPropertyName("latest-release-date")]
    public string latest_release_date { get; set; }

    [JsonPropertyName("latest-runtime")]
    public string latest_runtime { get; set; }

    [JsonPropertyName("latest-sdk")]
    public string latest_sdk { get; set; }

    [JsonPropertyName("support-phase")]
    public string support_phase { get; set; }

    [JsonPropertyName("release-type")]
    public string release_type { get; set; }

    [JsonPropertyName("eol-date")]
    public string eol_date { get; set; }

    [JsonPropertyName("lifecycle-policy")]
    public string lifecycle_policy { get; set; }

    [JsonPropertyName("releases")]
    public Releases[] releases { get; set; }

    [JsonPropertyName("signature")]
    public Signature signature { get; set; }
}

public class Releases
{
    [JsonPropertyName("release-date")]
    public string release_date { get; set; }

    [JsonPropertyName("release-version")]
    public string release_version { get; set; }

    [JsonPropertyName("security")]
    public bool security { get; set; }

    [JsonPropertyName("cve-list")]
    public Cve_list[] cve_list { get; set; }

    [JsonPropertyName("release-notes")]
    public string release_notes { get; set; }

    [JsonPropertyName("runtime")]
    public Runtime runtime { get; set; }

    [JsonPropertyName("sdk")]
    public Sdk sdk { get; set; }

    [JsonPropertyName("sdks")]
    public Sdk[] sdks { get; set; }

    [JsonPropertyName("aspnetcore-runtime")]
    public Aspnetcore_runtime aspnetcore_runtime { get; set; }

    [JsonPropertyName("windowsdesktop")]
    public Windowsdesktop windowsdesktop { get; set; }
}

public class Cve_list
{
    [JsonPropertyName("cve-id")]
    public string cve_id { get; set; }

    [JsonPropertyName("cve-url")]
    public string cve_url { get; set; }
}

public class Runtime
{
    [JsonPropertyName("version")]
    public string version { get; set; }

    [JsonPropertyName("version-display")]
    public string version_display { get; set; }

    [JsonPropertyName("files")]
    public Files[] files { get; set; }
}

public class Sdk
{
    [JsonPropertyName("version")]
    public string version { get; set; }

    [JsonPropertyName("version-display")]
    public string version_display { get; set; }

    [JsonPropertyName("runtime-version")]
    public string runtime_version { get; set; }

    [JsonPropertyName("files")]
    public Files[] files { get; set; }
}

public class Aspnetcore_runtime
{
    [JsonPropertyName("version")]
    public string version { get; set; }

    [JsonPropertyName("version-display")]
    public string version_display { get; set; }

    [JsonPropertyName("files")]
    public Files[] files { get; set; }
}

public class Windowsdesktop
{
    [JsonPropertyName("version")]
    public string version { get; set; }

    [JsonPropertyName("version-display")]
    public string version_display { get; set; }

    [JsonPropertyName("files")]
    public Files[] files { get; set; }
}

public class Files
{
    [JsonPropertyName("name")]
    public string name { get; set; }

    [JsonPropertyName("rid")]
    public string rid { get; set; }

    [JsonPropertyName("url")]
    public string url { get; set; }

    [JsonPropertyName("hash")]
    public string hash { get; set; }
}