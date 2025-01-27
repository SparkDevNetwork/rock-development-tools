namespace SparkDevNetwork.Framework.Builder;

class Program
{
    static async Task Main( string[] args )
    {
        var buildPath = Path.Combine( Path.GetTempPath(), "rock-framework-builder" );
        var builder = new RockBuilder( buildPath );

        try
        {
            var version = await builder.PromptForRockVersionAsync();
            var suffix = builder.PromptForPrereleaseSuffix( version );
            var packageVersion = new Semver.SemVersion( version.Version.Major,
                version.Version.Minor,
                version.Version.Patch,
                suffix != string.Empty ? suffix.Split( '.' ) : null );

            Console.WriteLine( $"Building in {buildPath}" );

            await builder.DownloadRockAsync( version );

            await builder.BuildProjectsAsync(
                "Rock.Enums",
                "Rock.ViewModels",
                "Rock.Common",
                "Rock.Lava.Shared",
                "Rock",
                "Rock.Rest",
                "Rock.JavaScript.Obsidian" );

            await builder.CreateNuGetPackagesAsync( packageVersion, [
                "Rock.Enums",
                "Rock.ViewModels",
                "Rock.Common",
                "Rock.Lava.Shared",
                "Rock",
                "Rock.Rest"
            ] );

            await builder.CreateObsidianFrameworkPackageAsync( packageVersion );
        }
        finally
        {
            builder.Cleanup();
        }
    }
}
