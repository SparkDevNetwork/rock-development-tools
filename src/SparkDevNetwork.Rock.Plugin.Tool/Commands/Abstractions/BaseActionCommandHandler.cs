using Microsoft.Extensions.Logging;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.Abstractions;

/// <summary>
/// Base handler for commands perform some action.
/// </summary>
/// <typeparam name="TOptions">The type of options used by the command.</typeparam>
abstract class BaseActionCommandHandler<TOptions>
    where TOptions : BaseActionCommandOptions
{
    /// <summary>
    /// The options for the command.
    /// </summary>
    protected TOptions Options { get; }

    /// <summary>
    /// The logger for this command instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Creates an action command handler.
    /// </summary>
    /// <param name="options">The options for this handler.</param>
    protected BaseActionCommandHandler( TOptions options )
    {
        Options = options;
        Logger = options.LoggerFactory.CreateLogger( GetType().Name );
    }

    /// <summary>
    /// Executes the command with the options obtained from the command line.
    /// </summary>
    /// <returns>A task that represents when the command has completed.</returns>
    public abstract Task<int> InvokeAsync();
}
