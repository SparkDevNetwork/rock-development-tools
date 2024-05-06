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

    /// <summary>
    /// Creates a new options instance with default values.
    /// </summary>
    public ActionCommandBaseOptions()
    {
    }

    /// <summary>
    /// Creates a new options instance that is a copy of the specified options.
    /// </summary>
    /// <param name="source">The source options to copy.</param>
    public ActionCommandBaseOptions( ActionCommandBaseOptions source )
    {
        LoggerFactory = source.LoggerFactory;
    }
}
