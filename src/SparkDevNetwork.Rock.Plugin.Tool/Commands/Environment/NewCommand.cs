using System.CommandLine;
using System.CommandLine.Invocation;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.Environment;

/// <summary>
/// Container for sub-commands related to working with development environments.
/// </summary>
class NewCommand : BaseModifyCommand<NewCommandOptions, NewCommandHandler>
{
    /// <summary>
    /// The option that defines the output directory of the new environment.
    /// </summary>
    private readonly Option<string?> _outputOption;

    /// <summary>
    /// Creates a command that will handle creating a new development
    /// environment.
    /// </summary>
    public NewCommand()
        : base( "new", "Create a new development environment." )
    {
        _outputOption = new Option<string?>( "--output", "Location to place the generated output." );
        _outputOption.AddAlias( "-o" );

        AddOption( _outputOption );
    }

    /// <inheritdoc/>
    protected override NewCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.Output = context.ParseResult.GetValueForOption( _outputOption );

        return options;
    }
}
