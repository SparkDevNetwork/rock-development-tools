using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

using Semver;

namespace SparkDevNetwork.Rock.Plugin.Tool;

/// <summary>
/// The root command handler to process the command line arguments.
/// </summary>
class RootAppCommand
{
    /// <summary>
    /// The root command for this handler.
    /// </summary>
    private readonly RootCommand _rootCommand;

    /// <summary>
    /// Creates a new command handler.
    /// </summary>
    public RootAppCommand()
    {
        var rootCommand = new RootCommand( "Plugin development tool for RockRMS" )
        {
            Name = "rock-plugin-tool"
        };

        SetupCreateCommand( rootCommand );
        SetupPackCommand( rootCommand );

        _rootCommand = rootCommand;
    }

    #region Methods

    /// <summary>
    /// Sets up the standard create plugin command.
    /// </summary>
    /// <param name="parentCommand">The parent command of this command.</param>
    private static void SetupCreateCommand( Command parentCommand )
    {
        var createCommand = new Command( "create", "Creates a new plugin following a standard template." );

        var organizationOption = new Option<string?>( "--organization", "The name of the organization that owns the plugin" );
        var organizationCodeOption = new Option<string?>( "--organization-code", "The namespace-style code used for the organization" );
        var nameOption = new Option<string?>( "--name", "The name of the plugin" );
        var rockVersionOption = new Option<string?>( "--rock-version", "The version of Rock to base the plugin on" )
            .FromAmong( Support.SupportedRockVersions );
        var rockWebOption = new Option<string?>( "--rock-web", "The path to the RockWeb folder" );
        var obsidianOption = new Option<bool?>( "--obsidian", "Determines if an Obsidian project is created" );
        var copyOption = new Option<bool?>( "--copy", "Copies the built artifacts to the RockWeb folder automatically" );

        createCommand.Add( organizationOption );
        createCommand.Add( organizationCodeOption );
        createCommand.Add( nameOption );
        createCommand.Add( rockVersionOption );
        createCommand.Add( rockWebOption );
        createCommand.Add( obsidianOption );
        createCommand.Add( copyOption );

        createCommand.SetHandler( async ( InvocationContext context ) =>
        {
            var commandOptions = new CreateCommandOptions
            {
                Organization = context.ParseResult.GetValueForOption( organizationOption ),
                OrganizationCode = context.ParseResult.GetValueForOption( organizationCodeOption ),
                PluginName = context.ParseResult.GetValueForOption( nameOption ),
                RockWebPath = context.ParseResult.GetValueForOption( rockWebOption ),
                Obsidian = context.ParseResult.GetValueForOption( obsidianOption ),
                Copy = context.ParseResult.GetValueForOption( copyOption )
            };

            if ( SemVersion.TryParse( context.ParseResult.GetValueForOption( rockVersionOption ), SemVersionStyles.Strict, out var version ) )
            {
                commandOptions.RockVersion = version;
            }

            context.ExitCode = await new CreatePluginCommand( commandOptions ).InvokeAsync();
        } );

        parentCommand.Add( createCommand );
    }

    /// <summary>
    /// Sets up the standard pack plugin command.
    /// </summary>
    /// <param name="parentCommand">The parent command of this command.</param>
    private void SetupPackCommand( Command parentCommand )
    {
        var packCommand = new Command( "pack", "Packs a plugin into a package that can be uploaded to the Rock shop" );

        parentCommand.Add( packCommand );
    }

    /// <summary>
    /// Parses and invokes a command or displays an error message.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The exist code for the application.</returns>
    public async Task<int> InvokeAsync( string[] args )
    {
        return await _rootCommand.InvokeAsync( args );
    }

    #endregion
}
