using System.Text.RegularExpressions;

using Semver;

using SparkDevNetwork.Framework.Builder.Executor;
using SparkDevNetwork.Framework.Builder.Git;
using SparkDevNetwork.Framework.Builder.UI;

using Spectre.Console;

namespace SparkDevNetwork.Framework.Builder;

/// <summary>
/// Provides methods used when building the framework packages.
/// </summary>
partial class RockBuilder
{
    /// <summary>
    /// The regular expression that will be used to match version tags
    /// in git refs.
    /// </summary>
    [GeneratedRegex( @"refs\/tags\/((\d+)\.(\d+)\.(\d+)([\.-].*)?)" )]
    private static partial Regex VersionRegExp();

    /// <summary>
    /// The path that Rock will be downloaded and built in.
    /// </summary>
    private readonly string _rockPath;

    /// <summary>
    /// A instance that will handle VS related commands.
    /// </summary>
    private readonly VisualStudio _visualStudio;

    /// <summary>
    /// The URL to use when accessing the Rock repository.
    /// </summary>
    public string RepositoryUrl { get; set; } = "https://github.com/SparkDevNetwork/Rock";

    public RockBuilder( string buildPath )
    {
        _rockPath = Path.Combine( buildPath, "Rock" );
        _visualStudio = new VisualStudio( buildPath );
    }

    /// <summary>
    /// Gets the possible version tags that can be used when building Rock.
    /// </summary>
    /// <returns>A list of <see cref="RockVersionTag"/> objects.</returns>
    public List<RockVersionTag> GetRockVersions()
    {
        var minimumVersion = new SemVersion( 1, 16, 0 );

        return GitCommand
            .ListRemoteReferences( RepositoryUrl, "refs/tags/[0-9]*.*" )
            .Select( r => new
            {
                Match = VersionRegExp().Match( r.Ref ),
                r.CommitHash
            } )
            .Where( r => r.Match.Success )
            .Select( r => new
            {
                r.CommitHash,
                Tag = r.Match.Groups[1].Value,
                Major = int.Parse( r.Match.Groups[2].Value ),
                Minor = int.Parse( r.Match.Groups[3].Value ),
                Patch = int.Parse( r.Match.Groups[4].Value ),
                Prerelease = ParsePrereleaseSuffix( r.Match.Groups[5].Value )
            } )
            .Select( v => new RockVersionTag
            {
                CommitHash = v.CommitHash,
                Version = new SemVersion( v.Major, v.Minor, v.Patch, v.Prerelease ),
                Tag = v.Tag
            } )
            .Where( v => v.Version.ComparePrecedenceTo( minimumVersion ) >= 0 )
            .ToList();
    }

    /// <summary>
    /// Prompts the user for the version of Rock to build.
    /// </summary>
    /// <param name="versions">The possible version numbers available.</param>
    /// <returns>The selected version.</returns>
    public RockVersionTag PromptForRockVersion()
    {
        var versions = GetRockVersions();

        var prompt = new SelectionPrompt<RockVersionTag>()
            .Title( "Build which version of Rock" )
            .PageSize( 10 )
            .MoreChoicesText( "[grey](Move up and down to reveal more)[/]" )
            .AddChoices( versions.OrderByDescending( v => v.Version ) );

        prompt.Converter = v => v.Version.ToString();

        return AnsiConsole.Prompt( prompt );
    }

    /// <summary>
    /// Prompts for an optional pre-release suffix to apply to the packages.
    /// </summary>
    /// <param name="version">The version that is going to be built.</param>
    /// <returns>A string to use as the version suffix or an empty string if none.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage( "Performance", "CA1822:Mark members as static", Justification = "In the future this may require instance data." )]
    public string PromptForPrereleaseSuffix( RockVersionTag version )
    {
        var useSuffixPrompt = new TextPrompt<bool>( "Use pre-release suffix" )
            .AddChoice( true )
            .AddChoice( false )
            .DefaultValue( !string.IsNullOrEmpty( version.Version.Prerelease ) )
            .WithConverter( choice => choice ? "y" : "n" );

        var useSuffix = AnsiConsole.Prompt( useSuffixPrompt );

        if ( !useSuffix )
        {
            return string.Empty;
        }

        var suffixPrompt = new TextPrompt<string>( "Pre-release suffix" )
            .DefaultValue( version.Version.Prerelease )
            .Validate( v =>
            {
                if ( !v.StartsWith( '-' ) )
                {
                    return ValidationResult.Success();
                }

                return ValidationResult.Error( "Do not start with a dash." );
            } );

        return AnsiConsole.Prompt( suffixPrompt );
    }

    /// <summary>
    /// Download the specified version of Rock into the path.
    /// </summary>
    /// <param name="version">The version to download.</param>
    /// <param name="path">The path to clone the repository into.</param>
    public void DownloadRock( RockVersionTag version )
    {
        if ( Directory.Exists( _rockPath ) )
        {
            DeleteRepository( _rockPath );
        }

        ProgressBar.Run( "Downloading Rock", 1, bar =>
        {
            try
            {
                var command = new CommandExecutor( "git",
                    "clone",
                    RepositoryUrl,
                    _rockPath,
                    "--progress",
                    "--depth",
                    "1",
                    "--branch",
                    version.Tag )
                {
                    ProgressFromStandardError = true,
                    ProgressReporter = new GitCloneProgressReporter( bar )
                };

                var commandResult = command.Execute();

                if ( commandResult.ExitCode != 0 )
                {
                    bar.Fail();
                }
            }
            catch
            {
                if ( Directory.Exists( _rockPath ) )
                {
                    DeleteRepository( _rockPath );
                }
                throw;
            }
        } );
    }

    /// <summary>
    /// Builds the specified project from the Rock solution.
    /// </summary>
    /// <param name="projectName">The name of the project to build.</param>
    /// <returns><c>true</c> if the project was built.</returns>
    public async Task<bool> BuildProjectAsync( string projectName )
    {
        var projectPath = Path.Combine( _rockPath, projectName );
        var projectExt = "csproj";

        if ( File.Exists( Path.Combine( projectPath, $"{projectName}.esproj" ) ) )
        {
            projectExt = "esproj";
        }

        var buildResult = await IndeterminateBar.Run( $"Building {projectName}", async bar =>
        {
            var commandResult = await _visualStudio.BuildAsync( [
                $"{projectName}.{projectExt}",
                "/p:Configuration=Release",
                "/nr:false"
            ], projectPath );

            if ( commandResult.ExitCode != 0 )
            {
                bar.Fail();
            }

            return commandResult;
        } );

        if ( buildResult!.ExitCode != 0 )
        {
            buildResult.WriteOutput();

            return false;
        }

        return true;
    }

    /// <summary>
    /// Builds the specified projects from the Rock solution.
    /// </summary>
    /// <param name="projectNames">The names of the projects to build.</param>
    /// <returns><c>true</c> if the projects were built.</returns>
    public async Task<bool> BuildProjectsAsync( params string[] projectNames )
    {
        var restoreResult = await IndeterminateBar.Run( "Restoring NuGet packages.", async bar =>
        {
            var commandResult = await _visualStudio.NuGetAsync( ["restore", "Rock.sln"], _rockPath );

            if ( commandResult.ExitCode != 0 )
            {
                bar.Fail();
            }

            return commandResult;
        } );

        foreach ( var projectName in projectNames )
        {
            if ( !await BuildProjectAsync( projectName ) )
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Cleanup any files that should be removed after a completed or failed
    /// attempt to build Rock.
    /// </summary>
    public void Cleanup()
    {
        DeleteRepository( _rockPath );
    }

    /// <summary>
    /// Deletes the local repository at the given path. This should be used
    /// instead of Directory.Delete() because the .git folder has files with
    /// special file attributes that prevent normal deletion.
    /// </summary>
    /// <param name="path">The local repository path to delete.</param>
    private static void DeleteRepository( string path )
    {
        if ( !Directory.Exists( path ) )
        {
            return;
        }

        var gitPath = Path.Combine( path, ".git" );

        if ( Directory.Exists( gitPath ) )
        {
            foreach ( string fileName in Directory.EnumerateFiles( gitPath, "*", SearchOption.AllDirectories ) )
            {
                var fileInfo = new FileInfo( fileName );

                fileInfo.Attributes = FileAttributes.Normal;
            }
        }

        Directory.Delete( path, true );
    }

    /// <summary>
    /// Parse a suffix on the version tag into a set of pre-release comonents.
    /// </summary>
    /// <param name="suffix">The suffix on the version tag.</param>
    /// <returns>A list of strings.</returns>
    private static List<string> ParsePrereleaseSuffix( string suffix )
    {
        var components = new List<string>();
        var component = string.Empty;

        if ( string.IsNullOrEmpty( suffix ) )
        {
            return components;
        }

        components.Add( "rc" );

        for ( int i = 0; i < suffix.Length; i++ )
        {
            var ch = suffix[i];

            if ( ch == '.' || ch == '-' )
            {
                if ( component != string.Empty )
                {
                    components.Add( component );
                }

                component = string.Empty;
            }
            else if ( component == string.Empty )
            {
                component += ch;
            }
            else
            {
                if ( char.IsDigit( component[0] ) == char.IsDigit( ch ) )
                {
                    component += ch;
                }
                else
                {
                    components.Add( component );
                    component = string.Empty;
                    component += ch;
                }
            }
        }

        if ( component != string.Empty )
        {
            components.Add( component );
        }


        return components;
    }
}
