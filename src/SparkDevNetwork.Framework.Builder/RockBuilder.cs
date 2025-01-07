using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using GlobExpressions;

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
    /// The path to download and build Rock in.
    /// </summary>
    private readonly string _buildPath;

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

    /// <summary>
    /// Creates a new instance of <see cref="RockBuilder"/>. 
    /// </summary>
    /// <param name="buildPath">The path to use when downloading and building.</param>
    public RockBuilder( string buildPath )
    {
        _buildPath = buildPath;
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

    /// <summary>
    /// Prepares the files required to create the NuGet packages.
    /// </summary>
    /// <param name="packageVersion">The package version number.</param>
    /// <param name="projectNames">The projects to be prepared.</param>
    private void PrepareNugetPackages( SemVersion packageVersion, string[] projectNames )
    {
        CopyTemplateFile( "Icon.png", Path.Combine( _buildPath, "Icon.png" ) );
        CopyTemplateFile( "LICENSE.md", Path.Combine( _buildPath, "LICENSE.md" ) );

        foreach ( var projectName in projectNames )
        {
            var nuspec = $"{projectName}.nuspec";

            CopyTextTemplate( nuspec, Path.Combine( _buildPath, nuspec ), packageVersion );
        }
    }

    /// <summary>
    /// Prepare the directory structure for building the rock-obsidian-framework
    /// pacakge with NPM.
    /// </summary>
    /// <param name="packageVersion">The version of the package to build.</param>
    /// <returns><c>true</c> if the package was successfully prepared.</returns>
    public bool PrepareObsidianFrameworkPackage( SemVersion packageVersion )
    {
        var frameworkBuildPath = Path.Combine( _rockPath,
            "Rock.JavaScript.Obsidian",
            "dist",
            "Framework" );
        var frameworkPath = Path.Combine( _rockPath,
            "Rock.JavaScript.Obsidian",
            "Framework" );
        var stagingPath = Path.Combine( _buildPath, "rock-obsidian-framework" );

        // Delete any left over staging files.
        if ( Directory.Exists( stagingPath ) )
        {
            Directory.Delete( stagingPath, true );
        }

        Directory.CreateDirectory( stagingPath );

        var error = ProgressBar.Run( "Preparing rock-obsidian-framework", 1, bar =>
        {
            // Get the built files, except the Libs files since those are
            // internal to Rock and should not be used by plugins.
            var builtFiles = Glob.Files( frameworkBuildPath, "**/*.d.ts" )
                .Where( f => !f.StartsWith( $"Libs{Path.DirectorySeparatorChar}" ) )
                .Select( f => new
                {
                    Source = Path.Combine( frameworkBuildPath, f ),
                    Target = f
                } );

            // Get the ViewModels files that aren't copied into the distribution
            // folder but should be part of the packge.
            var viewModelFiles = Glob.Files( frameworkPath, "ViewModels/**/*.d.ts" )
                .Select( f => new
                {
                    Source = Path.Combine( frameworkPath, f ),
                    Target = f
                } );

            // Get the ViewModels files that aren't copied into the distribution
            // folder but should be part of the packge.
            var typesFiles = Glob.Files( frameworkPath, "Types/**/*.d.ts" )
                .Select( f => new
                {
                    Source = Path.Combine( frameworkPath, f ),
                    Target = f
                } );

            var files = builtFiles
                .Union( viewModelFiles )
                .Union( typesFiles )
                .ToList();

            if ( files.Count == 0 )
            {
                bar.Fail();

                return "No files were found, perhaps the build fialed.";
            }

            bar.SetStep( 0, files.Count );

            foreach ( var file in files )
            {
                var source = file.Source;
                var destination = Path.Combine( stagingPath, "types", file.Target );
                var destinationDirectory = Path.GetDirectoryName( destination );

                if ( destinationDirectory == null )
                {
                    bar.Fail();

                    return "Destination directory was null.";
                }

                if ( !Directory.Exists( destinationDirectory ) )
                {
                    Directory.CreateDirectory( destinationDirectory );
                }

                File.Copy( source, destination );

                bar.NextStep();
            }

            // Read the Vue version from the rock project.
            var obsidianPackagePath = Path.Combine( _rockPath,
                "Rock.JavaScript.Obsidian",
                "package.json" );
            var obsidianPackageText = File.ReadAllText( obsidianPackagePath );
            var obsidianPackage = JsonSerializer.Deserialize<JsonNode>( obsidianPackageText );
            var vueVersion = obsidianPackage!["dependencies"]!["vue"]!.ToString();

            // Create the package.json file.
            var templateJson = ReadTextTemplate( "rock-obsidian-framework.json" )
                .Replace( "{{ RockVersion }}", packageVersion.ToString() )
                .Replace( "{{ VueVersion }}", vueVersion );

            File.WriteAllText( Path.Combine( stagingPath, "package.json" ), templateJson );

            // Copy additional template files.
            CopyTemplateFile( "tsconfig.base.json",
                Path.Combine( stagingPath, "tsconfig.base.json" ) );
            CopyTemplateFile( "LICENSE.md",
                Path.Combine( stagingPath, "LICENSE.md" ) );

            return null;
        } );

        if ( error != null )
        {
            Console.WriteLine( error );

            return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a single NuGet package for the specified project.
    /// </summary>
    /// <param name="projectName">The name of the project to package.</param>
    /// <returns><c>true</c> if the package was created.</returns>
    private async Task<bool> CreateNuGetPackageAsync( string projectName )
    {
        var nugetResult = await IndeterminateBar.Run( $"Packing {projectName}", async bar =>
        {
            var result = await _visualStudio.NuGetAsync( [
                "pack",
                $"{projectName}.nuspec",
                "-OutputDirectory",
                _buildPath
            ], _buildPath );

            if ( result.ExitCode != 0 )
            {
                bar.Fail();
            }

            return result;
        } );

        if ( nugetResult.ExitCode != 0 )
        {
            nugetResult.WriteOutput();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Packs the specified projects from the Rock solution.
    /// </summary>
    /// <param name="packageVersion">The package version number to build.</param>
    /// <param name="projectNames">The names of the projects to pack.</param>
    /// <returns><c>true</c> if the projects were packed.</returns>
    public async Task<bool> CreateNuGetPackagesAsync( SemVersion packageVersion, string[] projectNames )
    {
        PrepareNugetPackages( packageVersion, projectNames );

        foreach ( var projectName in projectNames )
        {
            if ( !await CreateNuGetPackageAsync( projectName ) )
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Creates a single NPM package for the specified project.
    /// </summary>
    /// <param name="packageVersion">The version of the package to build.</param>
    /// <returns><c>true</c> if the package was created.</returns>
    public bool CreateObsidianFrameworkPackage( SemVersion packageVersion )
    {
        var stagingPath = Path.Combine( _buildPath, "rock-obsidian-framework" );

        if ( !PrepareObsidianFrameworkPackage( packageVersion ) )
        {
            return false;
        }

        var npmResult = IndeterminateBar.Run( "Packing rock-obsidian-framework", bar =>
        {
            var result = _visualStudio.Npm( [
                "pack",
                "--pack-destination",
                ".."
            ], stagingPath );

            if ( result.ExitCode != 0 )
            {
                bar.Fail();
            }

            return result;
        } );

        if ( npmResult.ExitCode != 0 )
        {
            npmResult.WriteOutput();
            return false;
        }

        return true;
    }

    #region Templates

    /// <summary>
    /// Gets the resource name for the specified template filename.
    /// </summary>
    /// <param name="filename">The filename in the resources.</param>
    /// <returns>The full namespace and name of the resource.</returns>
    private static string GetResourceName( string filename )
    {
        return $"{typeof( RockBuilder ).Namespace}.Resources.{filename}";
    }

    /// <summary>
    /// Reads the text template from the resource stream.
    /// </summary>
    /// <param name="filename">The name of the template filename.</param>
    /// <returns>The contents of the resource.</returns>
    private static string ReadTextTemplate( string filename )
    {
        var resourceName = GetResourceName( filename );
        var stream = typeof( RockBuilder ).Assembly.GetManifestResourceStream( resourceName )
            ?? throw new Exception( $"Template {resourceName} not found in resource list." );
        using var reader = new StreamReader( stream );

        return reader.ReadToEnd();
    }

    /// <summary>
    /// Copies the text template to the destination path after applying the
    /// text replacement values.
    /// </summary>
    /// <param name="filename">The filename of the source template.</param>
    /// <param name="destinationPath">The full destination path and filename.</param>
    /// <param name="rockVersion">The version of Rock being packaged.</param>
    public static void CopyTextTemplate( string filename, string destinationPath, SemVersion rockVersion )
    {
        var text = ReadTextTemplate( filename );

        text = text.Replace( "{{ RockVersion }}", rockVersion.ToString() );

        File.WriteAllText( destinationPath, text );
    }

    /// <summary>
    /// Copies the template file without changes to the destination.
    /// </summary>
    /// <param name="filename">The filename of the source template.</param>
    /// <param name="destinationPath">The full path and filename to copy the source to.</param>
    public static void CopyTemplateFile( string filename, string destinationPath )
    {
        var resourceName = GetResourceName( filename );

        using var stream = typeof( RockBuilder ).Assembly.GetManifestResourceStream( resourceName )
            ?? throw new Exception( $"Template {resourceName} not found in resource list." );

        using var destinationStream = File.OpenWrite( destinationPath );

        stream.CopyTo( destinationStream );
    }

    #endregion
}
