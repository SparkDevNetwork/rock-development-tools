using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands;

/// <summary>
/// Command handler for <see cref="UpdateCommand"/>.
/// </summary>
/// <typeparam name="TOptions">The type of options used by the command.</typeparam>
class UpdateCommandHandler : Abstractions.BaseModifyCommandHandler<UpdateCommandOptions>
{
    /// <summary>
    /// Creates the action command handler.
    /// </summary>
    /// <param name="options">The options for this handler.</param>
    public UpdateCommandHandler( UpdateCommandOptions options )
        : base( options )
    {
    }

    /// <inheritdoc/>
    public override async Task<int> InvokeAsync()
    {
        var environmentDirectory = Options.Target ?? Directory.GetCurrentDirectory();

        var environment = Environment.Open(environmentDirectory, AnsiConsole.Console, Options.LoggerFactory);

        if ( environment is null )
        {
            return 1;
        }

        environment.IsDryRun = Options.DryRun;

        if ( environment.IsEnvironmentUpToDate() )
        {
            AnsiConsole.WriteLine( "Environment is up to date." );
            return 0;
        }

        return 0;
    }
}
