using System.CommandLine;
using System.CommandLine.Invocation;

using SparkDevNetwork.Rock.Plugin.Tool.CommandOptions;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands;

/// <summary>
/// Base implementation for commands that create new content and files on disk.
/// </summary>
/// <typeparam name="TOptions">The type of options used by the command.</typeparam>
abstract class CreateCommandBase<TOptions> : ActionCommandBase<TOptions>
    where TOptions : CreateCommandBaseOptions, new()
{
    /// <summary>
    /// The option that describes if this command should be a dry-run and not
    /// actually make any modifications.
    /// </summary>
    private readonly Option<bool> _dryRunOption;

    /// <summary>
    /// The option that describes if this command should overwrite any existing
    /// data in the process of its execution.
    /// </summary>
    private readonly Option<bool> _forceOption;

    /// <summary>
    /// Creates a command that will perform some action to create or modify data. 
    /// </summary>
    /// <param name="name">The primary name of the action.</param>
    /// <param name="description">The description of what the command will do.</param>
    public CreateCommandBase( string name, string description )
        : base( name, description )
    {
        _dryRunOption = new Option<bool>( "--dry-run", "Displays a summary of what would happen if the given command line were run." );
        _forceOption = new Option<bool>( "--force", "Forces content to be generated even if it would change existing files." );

        AddOption( _dryRunOption );
        AddOption( _forceOption );
     }

    /// <inheritdoc/>
    protected override TOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.DryRun = context.ParseResult.GetValueForOption( _dryRunOption );
        options.Force = context.ParseResult.GetValueForOption( _forceOption );

        return options;
    }
}
