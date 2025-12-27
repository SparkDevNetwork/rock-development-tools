using System.CommandLine;
using System.CommandLine.Invocation;

namespace SparkDevNetwork.Framework.Builder;

/// <summary>
/// The root application command handler.
/// </summary>
class RootAppCommand : RootCommand, ICommandHandler
{
    #region Fields

    /// <summary>
    /// The option for the repository URL.
    /// </summary>
    private readonly Option<string> _repositoryOption;

    /// <summary>
    /// The option for the git tag to build.
    /// </summary>
    private readonly Option<string?> _tagOption;

    /// <summary>
    /// The option for the pre-release suffix.
    /// </summary>
    private readonly Option<string?> _prereleaseOption;

    #endregion

    /// <summary>
    /// Creates a new instance of the <see cref="RootAppCommand"/> class.
    /// </summary>
    public RootAppCommand()
        : base( "Builds the Rock Framework packages for publishing to NuGet and NPM." )
    {
        _repositoryOption = new Option<string>( "--repository", "The URL of the Git repository to build from." );
        _repositoryOption.SetDefaultValue( "https://github.com/SparkDevNetwork/Rock" );

        _tagOption = new Option<string?>( "--tag", "The tag to build." );

        _prereleaseOption = new Option<string?>( "--prerelease", "The pre-release suffix to append to the version." );

        AddOption( _repositoryOption );
        AddOption( _tagOption );
        AddOption( _prereleaseOption );

        Handler = this;
    }

    /// <inheritdoc/>
    int ICommandHandler.Invoke( InvocationContext context )
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    async Task<int> ICommandHandler.InvokeAsync( InvocationContext context )
    {
        string repositoryUrl = context.ParseResult.GetValueForOption( _repositoryOption )!;
        var tag = context.ParseResult.GetValueForOption( _tagOption );
        var suffix = context.ParseResult.GetValueForOption( _prereleaseOption );
        var buildPath = Path.Combine( Path.GetTempPath(), "rock-framework-builder" );
        var builder = new RockBuilder( buildPath );

        try
        {
            bool result;
            var version = await builder.PromptForRockVersionAsync( tag );

            if ( suffix == null && version.Version.IsPrerelease )
            {
                suffix = version.Version.Prerelease;
            }

            suffix ??= builder.PromptForPrereleaseSuffix( version );

            var packageVersion = new Semver.SemVersion( version.Version.Major,
                version.Version.Minor,
                version.Version.Patch,
                suffix != string.Empty ? suffix.Split( '.' ) : null );

            Console.WriteLine( $"Building {packageVersion} in {buildPath}" );

            builder.Cleanup();

            await builder.DownloadRockAsync( version );
            var projectsToBuild = builder.GetProjectsFromSolution();

            result = await builder.BuildProjectsAsync( packageVersion, projectsToBuild );

            if ( !result )
            {
                return 1;
            }

            result = await builder.CreateNuGetPackagesAsync( packageVersion, [
                "Rock.Enums",
                "Rock.ViewModels",
                "Rock.Common",
                "Rock.Lava.Shared",
                "Rock",
                "Rock.Rest",
                "Rock.Blocks"
            ] );

            if ( !result )
            {
                return 1;
            }

            result = await builder.CreateObsidianFrameworkPackageAsync( packageVersion );

            if ( !result )
            {
                return 1;
            }

            if ( packageVersion.Major >= 18 )
            {
                result = await builder.CreateRockWebPackage( packageVersion );

                if ( !result )
                {
                    return 1;
                }
            }

            return 0;
        }
        finally
        {
            builder.Cleanup();
        }
    }
}
