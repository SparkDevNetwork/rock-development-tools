namespace SparkDevNetwork.Rock.DevTool.Commands.GenerateCommands;

/// <summary>
/// The options that are used by <see cref="NewCommand"/>.
/// </summary>
class ViewModelsCommandOptions : Abstractions.BaseModifyCommandOptions
{
    /// <summary>
    /// The path to create the TypeScript files in.
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// The assemblies that will be scanned for enums.
    /// </summary>
    public List<string> Assemblies { get; set; } = [];

    /// <summary>
    /// The namespaces that will be scanned for enums.
    /// </summary>
    public List<string> Namespaces { get; set; } = [];

    /// <summary>
    /// Creates a new options instance with default values.
    /// </summary>
    public ViewModelsCommandOptions()
    {
    }

    /// <summary>
    /// Creates a new options instance that is a copy of the specified options.
    /// </summary>
    /// <param name="source">The source options to copy.</param>
    public ViewModelsCommandOptions( EnumsCommandOptions source )
        : base( source )
    {
        Output = source.Output;
        Assemblies = source.Assemblies;
        Namespaces = source.Namespaces;
    }
}
