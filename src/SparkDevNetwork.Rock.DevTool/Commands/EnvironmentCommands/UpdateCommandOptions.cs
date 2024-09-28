namespace SparkDevNetwork.Rock.DevTool.Commands.EnvironmentCommands;

/// <summary>
/// The options that are used by <see cref="RockCommand"/>.
/// </summary>
class UpdateCommandOptions : Abstractions.BaseModifyCommandOptions
{
    /// <summary>
    /// The base URL to use when downloading environment files.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// The directory that contains the environment.
    /// </summary>
    public string? EnvironmentPath { get; set; }

    /// <summary>
    /// Creates a new options instance with default values.
    /// </summary>
    public UpdateCommandOptions()
    {
    }

    /// <summary>
    /// Creates a new options instance that is a copy of the specified options.
    /// </summary>
    /// <param name="source">The source options to copy.</param>
    public UpdateCommandOptions( UpdateCommandOptions source )
        : base( source )
    {
        Source = source.Source;
        EnvironmentPath = source.EnvironmentPath;
    }
}
