using System.CommandLine;
using System.CommandLine.Invocation;

using Fluid.Values;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.Abstractions;

/// <summary>
/// The base implementation for all commands that perform some action.
/// </summary>
/// <typeparam name="TOptions">The type of options used by the command.</typeparam>
abstract class BaseActionCommand<TOptions> : Command
    where TOptions : BaseActionCommandOptions, new()
{
    /// <summary>
    /// The option that describes if diagnostic details are logged.
    /// </summary>
    private readonly Option<bool> _diagOption;

    /// <summary>
    /// The options for the execution of the command.
    /// </summary>
    protected TOptions ExecuteOptions { get; private set; } = new();

    /// <summary>
    /// The logger for this command instance.
    /// </summary>
    protected ILogger Logger { get; private set; } = NullLogger.Instance;

    protected IAnsiConsole Console { get; }

    /// <summary>
    /// Creates a command that will perform some action.
    /// </summary>
    /// <param name="name">The primary name of the action.</param>
    /// <param name="description">The description of what the command will do.</param>
    public BaseActionCommand( string name, string description, IServiceProvider serviceProvider )
        : base( name, description )
    {
        Console = serviceProvider.GetRequiredService<IAnsiConsole>();

        _diagOption = new Option<bool>( "--diag", "Include debugging diagnostic information." );

        AddOption( _diagOption );

        this.SetHandler( async ctx =>
        {
            ExecuteOptions = GetOptions( ctx );

            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();

            if ( ExecuteOptions.Diagnostics && factory is DynamicLoggerFactory dynamicFactory )
            {
                dynamicFactory.IsEnabled = true;
            }

            Logger = factory.CreateLogger( GetType().FullName! );

            ctx.ExitCode = await ExecuteAsync();
        } );
    }

    /// <summary>
    /// <para>
    /// Gets the options from the command line invocation that will be passed
    /// to the <see cref="ExecuteAsync(TOptions)"/> method.
    /// </para>
    /// <para>
    /// Subclasses should call the base method and then set additional values
    /// and return that instance.
    /// </para>
    /// </summary>
    /// <param name="context">The context that describes the command line invocation.</param>
    /// <returns>The options object.</returns>
    protected virtual TOptions GetOptions( InvocationContext context )
    {
        var options = new TOptions
        {
            Diagnostics = context.ParseResult.GetValueForOption( _diagOption )
        };

        return options;
    }

    /// <summary>
    /// Executes the command with the options obtained from the command line.
    /// </summary>
    /// <returns>A task that represents when the command has completed.</returns>
    protected abstract Task<int> ExecuteAsync();
}
