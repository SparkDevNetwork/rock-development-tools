using System.CommandLine;
using System.CommandLine.Invocation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands;

/// <summary>
/// The base implementation for all commands that perform some action.
/// </summary>
/// <typeparam name="TOptions">The type of options used by the command.</typeparam>
abstract class BaseActionCommand<TOptions, THandler> : Command
    where TOptions : BaseActionCommandOptions, new()
    where THandler : BaseActionCommandHandler<TOptions>
{
    /// <summary>
    /// The option that describes if diagnostic details are logged.
    /// </summary>
    private readonly Option<bool> _diagOption;

    /// <summary>
    /// Creates a command that will perform some action.
    /// </summary>
    /// <param name="name">The primary name of the action.</param>
    /// <param name="description">The description of what the command will do.</param>
    public BaseActionCommand( string name, string description )
        : base( name, description )
    {
        _diagOption = new Option<bool>( "--diag", "Include debugging diagnostic information." );

        AddOption( _diagOption );

        this.SetHandler( async ctx =>
        {
            var handler = Activator.CreateInstance( typeof( THandler ), [GetOptions( ctx )] ) as THandler;

            if ( handler != null )
            {
                ctx.ExitCode = await handler.InvokeAsync();
            }
            else
            {
                ctx.ExitCode = 1;
            }
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
            LoggerFactory = CreateLoggerFactory( context )
        };

        return options;
    }

    /// <summary>
    /// Creates a new logger factory configured for the command line invocation.
    /// </summary>
    /// <param name="context">The context that describes the command line invocation.</param>
    /// <returns>An instance of <see cref="ILoggerFactory"/> for the invocation.</returns>
    private ILoggerFactory CreateLoggerFactory( InvocationContext context )
    {
        if ( !context.ParseResult.GetValueForOption( _diagOption ) )
        {
            return NullLoggerFactory.Instance;
        }

        return LoggerFactory.Create( config =>
        {
            config.AddSimpleConsole( options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "[HH:mm:ss.fff] ";
            } );

            config.SetMinimumLevel( LogLevel.Information );
        } );
    }
}
