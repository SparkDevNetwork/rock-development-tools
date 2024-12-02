using System.Text.Json.Serialization;

using Semver;

namespace SparkDevNetwork.Rock.DevTool.Data;

/// <summary>
/// Details about a single published version of the plugin.
/// </summary>
class PluginLockVersionData
{
    /// <summary>
    /// The version number of the plugin.
    /// </summary>
    [JsonPropertyName( "version" )]
    [JsonConverter( typeof( SemVersionConverter ) )]
    public SemVersion Version { get; set; } = new SemVersion( 0, 0, 0 );

    /// <summary>
    /// The files that were installed in the RockWeb folder.
    /// </summary>
    [JsonPropertyName( "installedFiles" )]
    public List<string> InstalledFiles { get; set; } = [];
}
