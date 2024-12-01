using System.CommandLine;
using System.CommandLine.Invocation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.Commands.Abstractions;

/// <summary>
/// The base implementation for all commands that perform some action.
/// </summary>
abstract class BaseActionCommand : Command
{
    #region Fields

    /// <summary>
    /// The option that describes if diagnostic details are logged.
    /// </summary>
    private readonly Option<bool> _diagOption;

    #endregion

    #region Properties

    /// <summary>
    /// The logger for this command instance.
    /// </summary>
    protected ILogger Logger { get; private set; } = NullLogger.Instance;

    /// <summary>
    /// The console object that should be used when reading or writing to
    /// the console.
    /// </summary>
    protected IAnsiConsole Console { get; }

    /// <summary>
    /// <c>true</c> if diagnostic output is enabled for this command.
    /// </summary>
    public bool Diagnostics { get; set; }

    #endregion

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
            GetOptions( ctx );

            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();

            if ( Diagnostics && factory is DynamicLoggerFactory dynamicFactory )
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
    /// to the <see cref="ExecuteAsync()"/> method.
    /// </para>
    /// <para>
    /// Subclasses should call the base method and then set additional values
    /// and return that instance.
    /// </para>
    /// </summary>
    /// <param name="context">The context that describes the command line invocation.</param>
    protected virtual void GetOptions( InvocationContext context )
    {
        Diagnostics = context.ParseResult.GetValueForOption( _diagOption );
    }

    /// <summary>
    /// Executes the command with the options obtained from the command line.
    /// </summary>
    /// <returns>A task that represents when the command has completed.</returns>
    protected abstract Task<int> ExecuteAsync();
}
