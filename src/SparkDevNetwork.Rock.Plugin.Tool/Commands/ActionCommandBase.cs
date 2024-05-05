using System.CommandLine;
using System.CommandLine.Invocation;

using Microsoft.Extensions.Logging;

using SparkDevNetwork.Rock.Plugin.Tool.Enums;
using SparkDevNetwork.Rock.Plugin.Tool.CommandOptions;
using Microsoft.Extensions.Logging.Abstractions;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands;

/// <summary>
/// The base implementation for all commands that perform some action.
/// </summary>
/// <typeparam name="TOptions">The type of options used by the command.</typeparam>
abstract class ActionCommandBase<TOptions> : Command
    where TOptions : ActionCommandBaseOptions, new()
{
    /// <summary>
    /// The option that describes the verbosity level of the command.
    /// </summary>
    private readonly Option<VerbosityOptions> _verbosityOption;

    /// <summary>
    /// The logger for this command instance.
    /// </summary>
    protected ILogger Logger { get; private set; } = NullLogger.Instance;

    /// <summary>
    /// Creates a command that will perform some action.
    /// </summary>
    /// <param name="name">The primary name of the action.</param>
    /// <param name="description">The description of what the command will do.</param>
    public ActionCommandBase( string name, string description )
        : base( name, description )
    {
        _verbosityOption = new Option<VerbosityOptions>( "--verbosity", "Sets the verbosity level. Allowed values are q[uiet], m[inimal], n[ormal], and diag[nostic]." );
        _verbosityOption.SetDefaultValue( VerbosityOptions.n );
        _verbosityOption.AddAlias( "-v" );

        AddOption( _verbosityOption );

        this.SetHandler( async ctx => ctx.ExitCode = await ExecuteAsync( GetOptions( ctx ) ) );
    }

    /// <summary>
    /// Executes the command with the options obtained from the command line.
    /// </summary>
    /// <param name="options">The command line options.</param>
    /// <returns>A task that represents when the command has completed.</returns>
    protected abstract Task<int> ExecuteAsync( TOptions options );

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
            LoggerFactory = CreateLoggerFactory( context )
        };

        var type = GetType();

        Logger = options.LoggerFactory.CreateLogger( type.FullName ?? type.Name );

        return options;
    }

    /// <summary>
    /// Creates a new logger factory configured for the command line invocation.
    /// </summary>
    /// <param name="context">The context that describes the command line invocation.</param>
    /// <returns>An instance of <see cref="ILoggerFactory"/> for the invocation.</returns>
    private ILoggerFactory CreateLoggerFactory( InvocationContext context )
    {
        return LoggerFactory.Create( config =>
        {
            config.AddSimpleConsole( options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "[HH:mm:ss.fff] ";
            } );

            config.SetMinimumLevel( GetMinimumLogLevel( context ) );
        } );
    }

    /// <summary>
    /// Gets the minimum <see cref="LogLevel"/> to use for the <see cref="ILogger"/>
    /// when executing the command.
    /// </summary>
    /// <param name="context">The context that describes the command line invocation.</param>
    /// <returns>The minimum log level a message must meet to be displayed.</returns>
    private LogLevel GetMinimumLogLevel( InvocationContext context )
    {
        var value = context.ParseResult.GetValueForOption( _verbosityOption );

        if ( value == VerbosityOptions.d )
        {
            return LogLevel.Debug;
        }
        else if ( value == VerbosityOptions.n )
        {
            return LogLevel.Information;
        }
        else if ( value == VerbosityOptions.m )
        {
            return LogLevel.Warning;
        }
        else
        {
            return LogLevel.Error;
        }
    }
}
