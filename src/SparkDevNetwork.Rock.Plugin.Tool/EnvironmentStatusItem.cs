namespace SparkDevNetwork.Rock.Plugin.Tool;

/// <summary>
/// Describes a single status item in the environment. This can describe the
/// status of either a plugin or the Rock installation.
/// </summary>
class EnvironmentStatusItem
{
    #region Properties

    /// <summary>
    /// <c>true</c> if this status item is for a plugin; <c>false</c> if it is
    /// for the Rock installation.
    /// </summary>
    public bool IsPlugin { get; }

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
    /// <param name="isPlugin"><c>true</c> if this item is a plugin.</param>
    public EnvironmentStatusItem( string name, bool isPlugin )
    {
        Name = name;
        Message = "is OK.";
        IsUpToDate = true;
        IsPlugin = isPlugin;
    }

    /// <summary>
    /// Creates a new status item that is out of date.
    /// </summary>
    /// <param name="name">The name of the item.</param>
    /// <param name="message">The message to be displayed after the name.</param>
    /// <param name="isPlugin"><c>true</c> if this item is a plugin.</param>
    public EnvironmentStatusItem( string name, string message, bool isPlugin )
    {
        Name = name;
        Message = message;
        IsUpToDate = false;
        IsPlugin = isPlugin;
    }
}
