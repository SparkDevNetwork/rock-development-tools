namespace SparkDevNetwork.Rock.DevTool.Commands.GenerateCommands;

/// <summary>
/// The options that are used by <see cref="NewCommand"/>.
/// </summary>
class EnumsCommandOptions : Abstractions.BaseModifyCommandOptions
{
    /// <summary>
    /// The path to create the TypeScript files in.
    /// </summary>
    public string Output { get; set; } = string.Empty;

    public List<string> Assemblies { get; set; } = [];

    public List<string> Namespaces { get; set; } = [];

    /// <summary>
    /// Creates a new options instance with default values.
    /// </summary>
    public EnumsCommandOptions()
    {
    }

    /// <summary>
    /// Creates a new options instance that is a copy of the specified options.
    /// </summary>
    /// <param name="source">The source options to copy.</param>
    public EnumsCommandOptions( EnumsCommandOptions source )
        : base( source )
    {
        Output = source.Output;
        Assemblies = source.Assemblies;
    }
}
