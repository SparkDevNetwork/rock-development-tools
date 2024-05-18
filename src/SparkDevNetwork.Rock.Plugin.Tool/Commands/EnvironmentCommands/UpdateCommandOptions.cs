namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands;

/// <summary>
/// The options that are used by <see cref="UpdateCommand"/>.
/// </summary>
class UpdateCommandOptions : Abstractions.BaseModifyCommandOptions
{
    /// <summary>
    /// The target directory that contains the environment.
    /// </summary>
    public string? Target { get; set; }

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
        Target = source.Target;
    }
}
