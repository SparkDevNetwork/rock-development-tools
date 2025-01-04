using System.Text.Json.Serialization;

using Semver;

namespace SparkDevNetwork.Rock.DevTool.Data;

/// <summary>
/// The structure of the plugin file.
/// </summary>
class PluginData
{
    /// <summary>
    /// The filename to use for plugin data.
    /// </summary>
    public const string Filename = "plugin.json";

    /// <summary>
    /// The details about the organization this plugin belongs to.
    /// </summary>
    [JsonPropertyName( "organization" )]
    public OrganizationData Organization { get; set; } = new OrganizationData();

    /// <summary>
    /// The name name of this plugin.
    /// </summary>
    [JsonPropertyName( "name" )]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The current version number of the plugin.
    /// </summary>
    [JsonPropertyName( "version" )]
    [JsonIgnore( Condition = JsonIgnoreCondition.WhenWritingNull )]
    [JsonConverter( typeof( SemVersionConverter ) )]
    public SemVersion? Version { get; set; }

    /// <summary>
    /// The set of files to include when packaging this plugin. The keys are
    /// a string that represents the path to the file(s) to be included relative
    /// to the plugin path. The values are the destination directory relative
    /// to the RockWeb folder. If the key is not a glob and specifies a single
    /// file then the value must either include a filename or end in a slash to
    /// indicate the same filename is kept.
    /// </summary>
    [JsonPropertyName( "include" )]
    [JsonIgnore( Condition = JsonIgnoreCondition.WhenWritingNull )]
    public Dictionary<string, string>? Include { get; set; }

    /// <summary>
    /// The set of files to exclude from the include patterns.
    /// </summary>
    [JsonPropertyName( "exclude" )]
    [JsonIgnore( Condition = JsonIgnoreCondition.WhenWritingNull )]
    public List<string>? Exclude { get; set; }

    /// <summary>
    /// The set of files to delete from the Rock server when this package
    /// is installed.
    /// </summary>
    [JsonPropertyName( "delete" )]
    [JsonIgnore( Condition = JsonIgnoreCondition.WhenWritingNull )]
    public List<string>? Delete { get; set; }

    /// <summary>
    /// Additional data in the JSON stream that we don't know about.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
