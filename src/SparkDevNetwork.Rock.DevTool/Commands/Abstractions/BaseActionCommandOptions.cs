namespace SparkDevNetwork.Rock.DevTool.Commands.Abstractions;

/// <summary>
/// Describes the options that are available to all action commands.
/// </summary>
class BaseActionCommandOptions
{
    /// <summary>
    /// <c>true</c> if diagnostic output is enabled for this command.
    /// </summary>
    public bool Diagnostics { get; set; }

    /// <summary>
    /// Creates a new options instance with default values.
    /// </summary>
    public BaseActionCommandOptions()
    {
    }

    /// <summary>
    /// Creates a new options instance that is a copy of the specified options.
    /// </summary>
    /// <param name="source">The source options to copy.</param>
    public BaseActionCommandOptions( BaseActionCommandOptions source )
    {
        Diagnostics = source.Diagnostics;
    }
}
