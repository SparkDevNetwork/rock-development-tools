using System.Text.Json.Serialization;

namespace SparkDevNetwork.Rock.DevTool.Data;

/// <summary>
/// The data that describes the organization in the environment file.
/// </summary>
class OrganizationData
{
    /// <summary>
    /// The name of the organization, this should not include any "Inc" or
    /// anything like that.
    /// </summary>
    [JsonPropertyName( "name" )]
    public string? Name { get; set; }

    /// <summary>
    /// The organization code that will be used for C# namespaces and things
    /// like that.
    /// </summary>
    [JsonPropertyName( "code" )]
    public string? Code { get; set; }

    /// <summary>
    /// Additional data in the JSON stream that we don't know about.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
