using System.CommandLine;
using System.CommandLine.Invocation;

using Semver;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.Plugin;

/// <summary>
/// Container for sub-commands related to working with plugins.
/// </summary>
class CreateCommand : BaseModifyCommand<CreateCommandOptions, CreateCommandHandler>
{
    /// <summary>
    /// The options that have been resolved for this command.
    /// </summary>
    private CreateCommandOptions _options = new();

    private readonly Option<string?> _organizationOption;

    private readonly Option<string?> _organizationCodeOption;

    private readonly Option<string?> _nameOption;

    private readonly Option<string?> _rockVersionOption;

    private readonly Option<string?> _rockWebOption;

    private readonly Option<bool?> _obsidianOption;

    private readonly Option<bool?> _copyOption;

    /// <summary>
    /// Creates a command that will handle creating a new Rock plugin.
    /// </summary>
    public CreateCommand()
        : base( "create", "Creates a new plugin following a standard template.." )
    {
        _organizationOption = new Option<string?>( "--organization", "The name of the organization that owns the plugin" );
        _organizationCodeOption = new Option<string?>( "--organization-code", "The namespace-style code used for the organization" );
        _nameOption = new Option<string?>( "--name", "The name of the plugin" );
        _rockVersionOption = new Option<string?>( "--rock-version", "The version of Rock to base the plugin on" )
            .FromAmong( Support.SupportedRockVersions );
        _rockWebOption = new Option<string?>( "--rock-web", "The path to the RockWeb folder" );
        _obsidianOption = new Option<bool?>( "--obsidian", "Determines if an Obsidian project is created" );
        _copyOption = new Option<bool?>( "--copy", "Copies the built artifacts to the RockWeb folder automatically" );

        AddOption( _organizationOption );
        AddOption( _organizationCodeOption );
        AddOption( _nameOption );
        AddOption( _rockVersionOption );
        AddOption( _rockWebOption );
        AddOption( _obsidianOption );
        AddOption( _copyOption );
    }

    /// <inheritdoc/>
    protected override CreateCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.Organization = context.ParseResult.GetValueForOption( _organizationOption );
        options.OrganizationCode = context.ParseResult.GetValueForOption( _organizationCodeOption );
        options.PluginName = context.ParseResult.GetValueForOption( _nameOption );
        options.RockWebPath = context.ParseResult.GetValueForOption( _rockWebOption );
        options.Obsidian = context.ParseResult.GetValueForOption( _obsidianOption );
        options.Copy = context.ParseResult.GetValueForOption( _copyOption );

        if ( SemVersion.TryParse( context.ParseResult.GetValueForOption( _rockVersionOption ), SemVersionStyles.Strict, out var version ) )
        {
            options.RockVersion = version;
        }

        return options;
    }
}
