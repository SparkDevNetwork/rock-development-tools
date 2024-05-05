namespace SparkDevNetwork.Rock.Plugin.Tool.CommandOptions;

/// <summary>
/// Describes the options that are available to all commands that create or
/// modify files.
/// </summary>
class CreateCommandBaseOptions : ActionCommandBaseOptions
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
}
