using System.Text.Json.Serialization;

namespace SparkDevNetwork.Rock.DevTool.Data;

/// <summary>
/// The data that describes the Rock instance that should be installed in
/// the environment.
/// </summary>
class RockData
{
    /// <summary>
    /// The version number to install in the environment. An empty string or
    /// missing value indicates that the Rock instance will be handled manually
    /// or via git reference.
    /// </summary>
    [JsonPropertyName( "version" )]
    public string? Version { get; set; }

    /// <summary>
    /// The URL to the git repository to install Rock from.
    /// </summary>
    [JsonPropertyName( "url" )]
    public string? Url { get; set; }

    /// <summary>
    /// The branch name to checkout in the Rock repository.
    /// </summary>
    [JsonPropertyName( "branch" )]
    public string? Branch { get; set; }

    /// <summary>
    /// Additional data in the JSON stream that we don't know about.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
