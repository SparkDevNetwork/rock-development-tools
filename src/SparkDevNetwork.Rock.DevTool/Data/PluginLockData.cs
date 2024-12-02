using System.Text.Json.Serialization;

namespace SparkDevNetwork.Rock.DevTool.Data;

/// <summary>
/// The data that describes the permanent historical data about the plugin.
/// </summary>
class PluginLockData
{
    /// <summary>
    /// The filename to use for plugin lock files.
    /// </summary>
    public const string Filename = "plugin-lock.json";

    /// <summary>
    /// The current lock file version supported by this tool.
    /// </summary>
    public const int CurrentLockFileVersion = 1;

    /// <summary>
    /// The version of the lock file. If this value is greater than
    /// <see cref="CurrentLockFileVersion"/> then the file cannot be processed
    /// and an error should be displayed.
    /// </summary>
    [JsonPropertyName( "lockFileVersion" )]
    public int LockFileVersion { get; set; } = CurrentLockFileVersion;

    /// <summary>
    /// The list of versions that have been published.
    /// </summary>
    [JsonPropertyName( "versions" )]
    public List<PluginLockVersionData> Versions { get; set; } = [];
}
