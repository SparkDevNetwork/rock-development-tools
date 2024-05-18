namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands;

/// <summary>
/// The options that are used by <see cref="NewCommand"/>.
/// </summary>
class NewCommandOptions : Abstractions.BaseModifyCommandOptions
{
    /// <summary>
    /// The path to create the new environment in.
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// The base URL to use when downloading environment files.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Creates a new options instance with default values.
    /// </summary>
    public NewCommandOptions()
    {
    }

    /// <summary>
    /// Creates a new options instance that is a copy of the specified options.
    /// </summary>
    /// <param name="source">The source options to copy.</param>
    public NewCommandOptions( NewCommandOptions source )
        : base( source )
    {
        Output = source.Output;
        Source = source.Source;
    }
}
