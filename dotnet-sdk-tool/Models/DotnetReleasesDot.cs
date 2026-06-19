namespace dotnet_sdk_tool_template.Models;

/// <summary>
/// https://dotnetcli.azureedge.net/dotnet/release-metadata/releases-index.json
/// </summary>
public class DotnetReleasesDot
{
    public Releases_index[] releases_index { get; set; }
    public Signature signature { get; set; }
}

public class Releases_index
{
    public string channel_version { get; set; }
    public string latest_release { get; set; }
    public string latest_release_date { get; set; }
    public bool security { get; set; }
    public string latest_runtime { get; set; }
    public string latest_sdk { get; set; }
    public string product { get; set; }
    public string support_phase { get; set; }
    public string release_type { get; set; }
    public string releases_json { get; set; }
    public string supported_os_json { get; set; }
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
    public string channel_version { get; set; }
    public string latest_release { get; set; }
    public string latest_release_date { get; set; }
    public string latest_runtime { get; set; }
    public string latest_sdk { get; set; }
    public string support_phase { get; set; }
    public string release_type { get; set; }
    public string eol_date { get; set; }
    public string lifecycle_policy { get; set; }
    public Releases[] releases { get; set; }
    public Signature signature { get; set; }
}

public class Releases
{
    public string release_date { get; set; }
    public string release_version { get; set; }
    public bool security { get; set; }
    public Cve_list[] cve_list { get; set; }
    public string release_notes { get; set; }
    public Runtime runtime { get; set; }
    public Sdk sdk { get; set; }
    public Sdks[] sdks { get; set; }
    public Aspnetcore_runtime aspnetcore_runtime { get; set; }
    public Windowsdesktop windowsdesktop { get; set; }
}

public class Cve_list
{
    public string cve_id { get; set; }
    public string cve_url { get; set; }
}

public class Runtime
{
    public string version { get; set; }
    public string version_display { get; set; }
    public string vs_version { get; set; }
    public string vs_mac_version { get; set; }
    public Files[] files { get; set; }
}

public class Files
{
    public string name { get; set; }
    public string rid { get; set; }
    public string url { get; set; }
    public string hash { get; set; }
}

public class Sdk
{
    public string version { get; set; }
    public string version_display { get; set; }
    public string runtime_version { get; set; }
    public string vs_version { get; set; }
    public string vs_mac_version { get; set; }
    public string vs_support { get; set; }
    public string vs_mac_support { get; set; }
    public string csharp_version { get; set; }
    public string fsharp_version { get; set; }
    public string vb_version { get; set; }
    public Files1[] files { get; set; }
}

public class Files1
{
    public string name { get; set; }
    public string rid { get; set; }
    public string url { get; set; }
    public string hash { get; set; }
}

public class Sdks
{
    public string version { get; set; }
    public string version_display { get; set; }
    public string runtime_version { get; set; }
    public string vs_version { get; set; }
    public string vs_mac_version { get; set; }
    public string vs_support { get; set; }
    public string vs_mac_support { get; set; }
    public string csharp_version { get; set; }
    public string fsharp_version { get; set; }
    public string vb_version { get; set; }
    public Files2[] files { get; set; }
}

public class Files2
{
    public string name { get; set; }
    public string rid { get; set; }
    public string url { get; set; }
    public string hash { get; set; }
}

public class Aspnetcore_runtime
{
    public string version { get; set; }
    public string version_display { get; set; }
    public string[] version_aspnetcoremodule { get; set; }
    public string vs_version { get; set; }
    public Files3[] files { get; set; }
}

public class Files3
{
    public string name { get; set; }
    public string rid { get; set; }
    public string url { get; set; }
    public string hash { get; set; }
    public string akams { get; set; }
}

public class Windowsdesktop
{
    public string version { get; set; }
    public string version_display { get; set; }
    public Files4[] files { get; set; }
}

public class Files4
{
    public string name { get; set; }
    public string rid { get; set; }
    public string url { get; set; }
    public string hash { get; set; }
}