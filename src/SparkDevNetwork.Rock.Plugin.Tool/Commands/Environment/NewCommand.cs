using System.CommandLine;
using System.CommandLine.Invocation;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.Environment;

/// <summary>
/// Container for sub-commands related to working with development environments.
/// </summary>
class NewCommand : Abstractions.BaseModifyCommand<NewCommandOptions, NewCommandHandler>
{
    /// <summary>
    /// The option that defines the output directory of the new environment.
    /// </summary>
    private readonly Option<string?> _outputOption;

    /// <summary>
    /// The base URL when downloading environment files.
    /// </summary>
    private readonly Option<string?> _sourceOption;

    /// <summary>
    /// Creates a command that will handle creating a new development
    /// environment.
    /// </summary>
    public NewCommand()
        : base( "new", "Create a new development environment." )
    {
        _outputOption = new Option<string?>( "--output", "Location to place the generated output." );
        _outputOption.AddAlias( "-o" );

        _sourceOption = new Option<string?>( "--source", "The base URL to use when downloading environment files." );

        AddOption( _outputOption );
        AddOption( _sourceOption );
    }

    /// <inheritdoc/>
    protected override NewCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.Output = context.ParseResult.GetValueForOption( _outputOption );
        options.Source = context.ParseResult.GetValueForOption( _sourceOption );

        return options;
    }
}
