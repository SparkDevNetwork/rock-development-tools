using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace SparkDevNetwork.Rock.DevTool;

/// <summary>
/// Checks to see if a new version of the tool is available.
/// </summary>
[SuppressMessage( "", "IO0002", Justification = "Not required for this class." )]
[SuppressMessage( "", "IO0003", Justification = "Not required for this class." )]
[SuppressMessage( "", "IO0006", Justification = "Not required for this class." )]
class VersionChecker
{
    /// <summary>
    /// The home directory of the current user.
    /// </summary>
    private static string HomeDirectory => Environment.GetFolderPath( Environment.SpecialFolder.UserProfile );

    /// <summary>
    /// The full path to the configuration file.
    /// </summary>
    private static string ConfigFile => Path.Combine( HomeDirectory, ".rock", "dev-tool.json" );

    /// <summary>
    /// The options to use when writing the configuration file.
    /// </summary>
    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Checks the current tool version against the latest published version
    /// and displays a message if there is a new version available.
    /// </summary>
    public static async Task CheckToolVersionAsync()
    {
        var cts = new CancellationTokenSource( 5000 );

        try
        {
            if ( !IsVersionCheckRequired() )
            {
                return;
            }

            // Update the date first, if the check throws an error we still only
            // want that to happen once per day.
            UpdateLastVersionCheckDate();

            var currentVersionInfo = typeof( VersionChecker )
                .Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "0";
            var currentVersion = new NuGetVersion( currentVersionInfo );

            var latestVersion = await GetLatestVersionFromNuGet( currentVersion.IsPrerelease, cts.Token );

            if ( latestVersion.CompareTo( currentVersion ) > 0 )
            {
                var prerelease = latestVersion.IsPrerelease ? " --prerelease" : string.Empty;

                Console.WriteLine( "------------------------------------------------------------" );
                Console.WriteLine( "A new version of the tool was found online." );
                Console.WriteLine( $"Latest version: {latestVersion}" );
                Console.WriteLine( $"Current version: {currentVersion}" );
                Console.WriteLine( $"Run 'dotnet tool install --global sparkdevnetwork.rock.devtool{prerelease}' to update." );
                Console.WriteLine( "------------------------------------------------------------" );
                Console.WriteLine();
            }
        }
        catch ( TaskCanceledException )
        {
            return;
        }
        catch ( Exception ex )
        {
            Console.WriteLine( "Encountered error trying to check for new tool version:" );
            Console.WriteLine( $"  {ex.Message}" );
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Gets the latest version available of the tool on NuGet.
    /// </summary>
    /// <param name="includePrerelease">Indicates if pre-release versions should be included.</param>
    /// <param name="cancellationToken">A token that indicates when the network request should abort.</param>
    /// <returns>The latest version of the tool on NuGet.</returns>
    /// <exception cref="Exception"></exception>
    private static async Task<NuGetVersion> GetLatestVersionFromNuGet( bool includePrerelease, CancellationToken cancellationToken )
    {
        var repo = Repository.Factory.GetCoreV3( "https://api.nuget.org/v3/index.json" );
        var resource = await repo.GetResourceAsync<PackageSearchResource>( cancellationToken );
        var searchFilter = new SearchFilter( includePrerelease: includePrerelease );

        var searchResults = await resource.SearchAsync(
            "SparkDevNetwork.Rock.DevTool",
            searchFilter,
            0,
            1,
            NullLogger.Instance,
            cancellationToken );

        var package = searchResults.SingleOrDefault()
            ?? throw new Exception( "Unable to find package on NuGet." );
        var versions = await package.GetVersionsAsync();

        if ( versions == null || !versions.Any() )
        {
            throw new Exception( "Unable to find latest version on NuGet." );
        }

        return versions.OrderByDescending( v => v.Version )
            .Select( v => v.Version )
            .First();
    }

    /// <summary>
    /// Determines if a version check is required based on the last date we
    /// checked. The check only happens once per day.
    /// </summary>
    /// <returns><c>true</c> if a check should be performed; otherwise <c>false</c>.</returns>
    private static bool IsVersionCheckRequired()
    {
        if ( !File.Exists( ConfigFile ) )
        {
            return true;
        }

        try
        {
            var json = File.ReadAllText( ConfigFile );
            var data = JsonSerializer.Deserialize<JsonObject>( json );

            if ( data == null )
            {
                return true;
            }

            if ( !data.ContainsKey( "lastCheckDate" ) )
            {
                return true;
            }

            var lastCheckDateString = data["lastCheckDate"]?.ToString();

            if ( lastCheckDateString == null || !DateTime.TryParse( lastCheckDateString, out var lastCheckDate ) )
            {
                return true;
            }

            return lastCheckDate.Date < DateTime.Today;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Updates the last check date in the configuration file.
    /// </summary>
    private static void UpdateLastVersionCheckDate()
    {
        Directory.CreateDirectory( Path.GetDirectoryName( ConfigFile )! );

        try
        {
            string json;
            JsonObject data;

            try
            {
                json = File.ReadAllText( ConfigFile );
                data = JsonSerializer.Deserialize<JsonObject>( json ) ?? [];
            }
            catch
            {
                data = [];
            }

            data["lastCheckDate"] = DateTime.Now.ToString( "O" );

            json = JsonSerializer.Serialize( data, _writeOptions );

            File.WriteAllText( ConfigFile, json );
        }
        catch
        {
            // Ignored.
        }
    }
}
