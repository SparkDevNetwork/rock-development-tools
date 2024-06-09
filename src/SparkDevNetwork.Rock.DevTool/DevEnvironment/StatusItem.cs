namespace SparkDevNetwork.Rock.DevTool.DevEnvironment;

/// <summary>
/// Describes a single status item in the environment.
/// </summary>
class StatusItem
{
    #region Properties

    /// <summary>
    /// <c>true</c> if this item is up to date with the environment configuration.
    /// </summary>
    public bool IsUpToDate { get; }

    /// <summary>
    /// A message that will be displayed after the <see cref="Name"/>.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The name of the item.
    /// </summary>
    public string Name { get; }

    #endregion

    /// <summary>
    /// Creates a new status item that is up to date.
    /// </summary>
    /// <param name="name">The name of the item.</param>
    public StatusItem( string name )
    {
        Name = name;
        Message = "is up-to-date.";
        IsUpToDate = true;
    }

    /// <summary>
    /// Creates a new status item that is out of date.
    /// </summary>
    /// <param name="name">The name of the item.</param>
    /// <param name="message">The message to be displayed after the name.</param>
    public StatusItem( string name, string message )
    {
        Name = name;
        Message = message;
        IsUpToDate = false;
    }
}
