namespace SparkDevelopmentNetwork.Rock.Plugin.Tool;

/// <summary>
/// The options that are used by <see cref="CreatePluginCommand"/>.
/// </summary>
class CreateCommandOptions
{
    /// <summary>
    /// The name of the organization such as <c>Rock Solid Church</c>.
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// The namespace code of the organization such as <c>com.rocksolidchurch</c>.
    /// </summary>
    public string? OrganizationCode { get; set; }

    /// <summary>
    /// The name of the plugin such as <c>Sample Data</c>.
    /// </summary>
    public string? PluginName { get; set; }

    /// <summary>
    /// The namespace code to use for the plugin name such as <c>SampleData</c>.
    /// </summary>
    public string? PluginCode => PluginName?.Replace( " ", "" );

    /// <summary>
    /// The version number of Rock the plugin will target such as <c>1.16.0</c>.
    /// </summary>
    public Semver.SemVersion? RockVersion { get; set; }

    /// <summary>
    /// The relative path to the RockWeb folder such as <c>../RockWeb</c>.
    /// </summary>
    public string? RockWebPath { get; set; }

    /// <summary>
    /// Determines if the Obsidian project will be created.
    /// </summary>
    public bool? Obsidian { get; set; }

    /// <summary>
    /// Determines if the build artifacts will be copied to <see cref="RockWebPath" />.
    /// </summary>
    public bool? Copy { get; set; }
}
