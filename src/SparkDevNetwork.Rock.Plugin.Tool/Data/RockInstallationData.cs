using System.Text.Json.Serialization;

namespace SparkDevNetwork.Rock.Plugin.Tool.Data;

/// <summary>
/// The data that describes the current Rock instance installed. This lives
/// inside a <c>.rock.json</c> file in the Rock directory.
/// </summary>
class RockInstallationData
{
    /// <summary>
    /// A dictionary of installed files. Each entry key is the relative path
    /// to the file with <c>/</c> as the path separator. The value is the
    /// SHA1 hash of the file when it was installed.
    /// </summary>
    [JsonPropertyName( "files" )]
    public Dictionary<string, string>? Files { get; set; }
}
