using Microsoft.Extensions.Logging;

namespace SparkDevNetwork.Rock.Plugin.Tool.CommandOptions;

/// <summary>
/// Describes the options that are available to all action commands.
/// </summary>
class ActionCommandBaseOptions
{
    /// <summary>
    /// The factory that will be used to create <see cref="ILogger"/> instances
    /// during the execution of the command.
    /// </summary>
    public required ILoggerFactory LoggerFactory { get; init; }
}
