namespace SparkDevNetwork.Rock.Plugin.Tool.CommandOptions.Environment;

/// <summary>
/// The options that are used by <see cref="Commands.Environment.NewCommand"/>.
/// </summary>
class NewCommandOptions : ModifyCommandBaseOptions
{
    /// <summary>
    /// The path to create the new environment in.
    /// </summary>
    public string? Output { get; set; }

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
    }
}
