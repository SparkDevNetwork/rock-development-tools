using System.Reflection;
using System.Text.Json;

using LibGit2Sharp;

using Microsoft.Extensions.Logging;

using Semver;

using SparkDevNetwork.Rock.Plugin.Tool.Data;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.Environment;

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
        var environmentFile = Path.Combine( environmentDirectory, EnvironmentData.Filename );

        if ( !File.Exists( environmentFile ) )
        {
            AnsiConsole.MarkupLineInterpolated( $"No environment file was found at [cyan]{environmentFile}[/]." );
            return 1;
        }

        var json = File.ReadAllText( environmentFile );
        var environment = JsonSerializer.Deserialize<EnvironmentData>( json );

        if ( environment == null )
        {
            AnsiConsole.MarkupLineInterpolated( $"Invalid environment configuration found in [cyan]{environmentFile}[/]." );
            return 1;
        }

        if ( IsEnvironmentUpToDate( environmentDirectory, environment ) )
        {
            AnsiConsole.WriteLine( "Environment is up to date." );
            return 0;
        }

        return 0;
    }

    /// <summary>
    /// Checks if the environment is up to date with the configuration.
    /// </summary>
    /// <param name="environmentDirectory">The directory that contains the environment.</param>
    /// <param name="environment">The environment configuration.</param>
    /// <returns><c>true</c> if the environment is already up to date; otherwise <c>false</c>.</returns>
    private bool IsEnvironmentUpToDate( string environmentDirectory, EnvironmentData environment )
    {
        if ( !IsRockUpToDate( environmentDirectory, environment.Rock ) )
        {
            return false;
        }

        if ( environment.Plugins != null )
        {
            foreach ( var plugin in environment.Plugins )
            {
                if ( !IsPluginUpToDate( environmentDirectory, plugin ) )
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if the Rock installation is up to date. This is a best guess
    /// based on the version number of the Rock.dll file.
    /// </summary>
    /// <param name="environmentDirectory">The directory that contains the environment.</param>
    /// <param name="rock">The Rock environment configuration.</param>
    /// <returns><c>true</c> if the Rock version number is correct; otherwise <c>false</c>.</returns>
    private bool IsRockUpToDate( string environmentDirectory, RockData? rock )
    {
        if ( rock == null || rock.Version == "custom" )
        {
            return true;
        }

        if ( !SemVersion.TryParse( rock.Version, SemVersionStyles.Strict, out var version ) )
        {
            Logger.LogError( "Unable to parse Rock version number '{version}'.", rock.Version );
            return false;
        }

        var rockDllPath = Path.Combine( environmentDirectory, "Rock", "RockWeb", "Bin", "Rock.dll" );
        if ( !File.Exists( rockDllPath ) )
        {
            Logger.LogInformation( "No Rock assembly was found at {filename}.", rockDllPath );
            return false;
        }

        var asmName = AssemblyName.GetAssemblyName( rockDllPath );

        if ( asmName.Version == null )
        {
            Logger.LogError( "No version number found in Rock assembly." );
            return false;
        }

        if ( version.Major < 2 )
        {
            var doesVersionMatch = version.Major == asmName.Version.Major
                && version.Minor == asmName.Version.Minor
                && version.Patch == asmName.Version.Build;

            if ( !doesVersionMatch )
            {
                Logger.LogInformation( "Rock assembly version number {rockVersion} does not match expected version {expectedVersion}.", version, asmName.Version );
                return false;
            }
        }
        else
        {
            var doesVersionMatch = version.Major == asmName.Version.Major
                && version.Minor == asmName.Version.Minor;

            if ( !doesVersionMatch )
            {
                Logger.LogInformation( "Rock assembly version number {rockVersion} does not match expected version {expectedVersion}.", version, asmName.Version );
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if the plugin is up to date with the environment configuration.
    /// </summary>
    /// <param name="environmentDirectory">The directory that contains the environment.</param>
    /// <param name="plugin">The plugin configuration.</param>
    /// <returns><c>true</c> if the plugin branch is correct; otherwise <c>false</c>.</returns>
    private bool IsPluginUpToDate( string environmentDirectory, PluginData plugin )
    {
        if ( plugin.Path == null || plugin.Url == null )
        {
            if ( plugin.Path == null )
            {
                Logger.LogError( "Defined plugin is missing path." );
            }
            else
            {
                Logger.LogError( "Plugin {name} is missing url.", plugin.Path );
            }

            return false;
        }

        var pluginDirectory = Path.Combine( environmentDirectory, plugin.Path.Replace( '/', Path.PathSeparator ) );

        if ( !Directory.Exists( pluginDirectory ) )
        {
            Logger.LogInformation( "Plugin {path} is missing.", plugin.Path );
            return false;
        }

        if ( !Repository.IsValid( pluginDirectory ) )
        {
            Logger.LogError( "Plugin {path} is not a git repository.", plugin.Path );
            return false;
        }

        if ( plugin.Branch == null )
        {
            return true;
        }

        var repository = new Repository( pluginDirectory );
        var reference = repository.Head.Reference.TargetIdentifier;

        if ( !reference.StartsWith( "refs/heads/" ) )
        {
            Logger.LogInformation( "Plugin {path} is not on a branch.", plugin.Path );
            return false;
        }

        var branch = reference.Substring( 11 );

        if ( plugin.Branch != branch )
        {
            Logger.LogInformation( "Plugin {path} is on branch {repoBranch} instead of {expectedBranch}.", plugin.Path, branch, plugin.Branch );
            return false;
        }

        return true;
    }
}
