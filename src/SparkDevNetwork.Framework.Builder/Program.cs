namespace SparkDevNetwork.Framework.Builder;

class Program
{
    static async Task Main( string[] args )
    {
        var buildPath = Path.Combine( Path.GetTempPath(), "rock-framework-builder" );
        var builder = new RockBuilder( buildPath );

        try
        {
            var version = builder.PromptForRockVersion();
            var suffix = builder.PromptForPrereleaseSuffix( version );

            Console.WriteLine( $"Building in {buildPath}" );

            builder.DownloadRock( version );

            await builder.BuildProjectsAsync(
                "Rock.Enums",
                "Rock.ViewModels",
                "Rock.Common",
                "Rock.Lava.Shared",
                "Rock",
                "Rock.Rest",
                "Rock.JavaScript.Obsidian" );
        }
        finally
        {
            builder.Cleanup();
        }
    }
}
