namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.PluginCommands;

/// <summary>
/// The options that are used by <see cref="NewCommand"/>.
/// </summary>
class ConfigureCommandOptions : Abstractions.BaseModifyCommandOptions
{
    /// <summary>
    /// The directory that contains the environment.
    /// </summary>
    public string? EnvironmentPath { get; set; }

    /// <summary>
    /// The URL to configure the plugin to use or <c>null</c> to leave unchanged.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// The branch name to configure the plugin to use or <c>null</c> to leave
    /// unchanged.
    /// </summary>
    public string? Branch { get; set; }

    /// <summary>
    /// The relative path to the plugin.
    /// </summary>
    public string PluginPath { get; set; } = null!;

    /// <summary>
    /// Creates a new options instance with default values.
    /// </summary>
    public ConfigureCommandOptions()
    {
    }

    /// <summary>
    /// Creates a new options instance that is a copy of the specified options.
    /// </summary>
    /// <param name="source">The source options to copy.</param>
    public ConfigureCommandOptions( ConfigureCommandOptions source )
        : base( source )
    {
        EnvironmentPath = source.EnvironmentPath;
        Url = source.Url;
        Branch = source.Branch;
        PluginPath = source.PluginPath;
    }
}
