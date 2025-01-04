namespace SparkDevNetwork.Rock.DevTool.DevEnvironment;

/// <summary>
/// Describes a single status item in the environment for the Rock installation.
/// </summary>
class RockStatusItem : StatusItem
{
    #region Properties

    /// <summary>
    /// The status of each individual file that is part of the installation.
    /// </summary>
    public List<StatusItem> Files { get; }

    #endregion

    /// <summary>
    /// Creates a new status item that is up to date.
    /// </summary>
    /// <param name="files">The status of each individual file.</param>
    public RockStatusItem( List<StatusItem> files )
        : base( "Rock" )
    {
        Files = files;
    }

    /// <summary>
    /// Creates a new status item that is out of date.
    /// </summary>
    /// <param name="message">The message to be displayed after the name.</param>
    /// <param name="files">The status of each individual file.</param>
    public RockStatusItem( string message, List<StatusItem>? files )
        : base( "Rock", message )
    {
        Files = files ?? new List<StatusItem>();
    }
}
