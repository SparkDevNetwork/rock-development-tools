namespace SparkDevNetwork.Rock.Plugin.Tool.CommandOptions.Plugin;

/// <summary>
/// The options that are used by <see cref="Commands.Plugin.CreateCommand"/>.
/// </summary>
class CreateCommandOptions : CreateCommandBaseOptions
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

    /// <summary>
    /// Creates a new options instance with default values.
    /// </summary>
    public CreateCommandOptions()
    {
    }

    /// <summary>
    /// Creates a new options instance that is a copy of the specified options.
    /// </summary>
    /// <param name="source">The source options to copy.</param>
    public CreateCommandOptions( CreateCommandOptions source )
        : base( source )
    {
        Organization = source.Organization;
        OrganizationCode = source.OrganizationCode;
        PluginName = source.PluginName;
        RockVersion = source.RockVersion;
        RockWebPath = source.RockWebPath;
        Obsidian = source.Obsidian;
        Copy = source.Copy;
    }
}
