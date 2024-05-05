using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    public ILoggerFactory LoggerFactory { get; set; } = NullLoggerFactory.Instance;
}
