using SparkDevNetwork.Rock.Plugin.Tool.Data;

namespace SparkDevNetwork.Rock.Plugin.Tool;

/// <summary>
/// Describes a single status item in the environment. This can describe the
/// status of either a plugin or the Rock installation.
/// </summary>
class EnvironmentStatusItem
{
    #region Properties

    /// <summary>
    /// The plugin data if this item represents a plugin or <c>null</c> if
    /// this item represents the Rock instance.
    /// </summary>
    public PluginData? Plugin { get; }

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
    /// <param name="plugin">The plugin definition or <c>null</c> if this is the Rock instance.</param>
    public EnvironmentStatusItem( string name, PluginData? plugin )
    {
        Name = name;
        Message = "is OK.";
        IsUpToDate = true;
        Plugin = plugin;
    }

    /// <summary>
    /// Creates a new status item that is out of date.
    /// </summary>
    /// <param name="name">The name of the item.</param>
    /// <param name="message">The message to be displayed after the name.</param>
    /// <param name="plugin">The plugin definition or <c>null</c> if this is the Rock instance.</param>
    public EnvironmentStatusItem( string name, string message, PluginData? plugin )
    {
        Name = name;
        Message = message;
        IsUpToDate = false;
        Plugin = plugin;
    }
}
