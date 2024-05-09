namespace SparkDevNetwork.Rock.Plugin.Tool.Commands;

/// <summary>
/// Describes the options that are available to all commands that create or
/// modify content.
/// </summary>
class BaseModifyCommandOptions : BaseActionCommandOptions
{
    /// <summary>
    /// Will be <c>true</c> if the command should not actually make any
    /// modifications but instead just report what would have been done.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Will be <c>true</c> if the command should overwrite anything that
    /// would otherwise cause the command to abort without making changes.
    /// </summary>
    public bool Force { get; set; }

    /// <summary>
    /// Creates a new options instance with default values.
    /// </summary>
    public BaseModifyCommandOptions()
    {
    }

    /// <summary>
    /// Creates a new options instance that is a copy of the specified options.
    /// </summary>
    /// <param name="source">The source options to copy.</param>
    public BaseModifyCommandOptions( BaseModifyCommandOptions source )
        : base( source )
    {
        DryRun = source.DryRun;
        Force = source.Force;
    }
}
