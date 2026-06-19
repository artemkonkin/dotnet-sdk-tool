using System.Text.Json.Serialization;

namespace dotnet_sdk_tool_template.Models;

/// <summary>
/// Root of Assets/legacy_framework_versions.json.
/// </summary>
public class LegacyFrameworkFile
{
    [JsonPropertyName("modern_dotnet_registry")]
    public string modern_dotnet_registry { get; set; }

    [JsonPropertyName("legacy_frameworks")]
    public LegacyFramework[] legacy_frameworks { get; set; }
}

/// <summary>
/// A single legacy .NET Framework version (Windows-only).
/// </summary>
public class LegacyFramework
{
    [JsonPropertyName("version")]
    public string version { get; set; }

    [JsonPropertyName("product")]
    public string product { get; set; }

    [JsonPropertyName("release_date")]
    public string release_date { get; set; }

    [JsonPropertyName("windows_only")]
    public bool windows_only { get; set; }

    /// <summary>"offline_installer" or "windows_feature".</summary>
    [JsonPropertyName("install_type")]
    public string install_type { get; set; }

    [JsonPropertyName("download_url")]
    public string download_url { get; set; }

    /// <summary>DISM feature name for install_type == "windows_feature" (e.g. "NetFx3").</summary>
    [JsonPropertyName("dism_name")]
    public string dism_name { get; set; }
}
