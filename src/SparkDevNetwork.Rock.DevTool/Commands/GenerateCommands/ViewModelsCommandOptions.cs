namespace SparkDevNetwork.Rock.DevTool.Commands.GenerateCommands;

/// <summary>
/// The options that are used by <see cref="ViewModelsCommand"/>.
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
    public string Assembly { get; set; } = string.Empty;

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
        Assembly = source.Assembly;
    }
}
