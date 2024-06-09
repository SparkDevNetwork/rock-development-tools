using SparkDevNetwork.Rock.DevTool.Data;

namespace SparkDevNetwork.Rock.DevTool.DevEnvironment;

/// <summary>
/// Describes a single status item in the environment for a plugin.
/// </summary>
class PluginStatusItem : StatusItem
{
    #region Properties

    /// <summary>
    /// The plugin data if this item represents a plugin or <c>null</c> if
    /// this item represents the Rock instance.
    /// </summary>
    public PluginData? Plugin { get; }

    #endregion

    /// <summary>
    /// Creates a new status item that is up to date.
    /// </summary>
    /// <param name="name">The name of the item.</param>
    /// <param name="plugin">The plugin definition or <c>null</c> if this is the Rock instance.</param>
    public PluginStatusItem( string name, PluginData? plugin )
        : base( name )
    {
        Plugin = plugin;
    }

    /// <summary>
    /// Creates a new status item that is out of date.
    /// </summary>
    /// <param name="name">The name of the item.</param>
    /// <param name="message">The message to be displayed after the name.</param>
    /// <param name="plugin">The plugin definition or <c>null</c> if this is the Rock instance.</param>
    public PluginStatusItem( string name, string message, PluginData? plugin )
        : base( name, message )
    {
        Plugin = plugin;
    }
}
