namespace SparkDevNetwork.Framework.Builder;

class Program
{
    static async Task Main( string[] args )
    {
        var buildPath = Path.Combine( Path.GetTempPath(), "rock-framework-builder" );
        var builder = new RockBuilder( buildPath );

        Console.WriteLine( $"Building in {buildPath}" );

        try
        {
            var version = builder.PromptForRockVersion();
            var suffix = builder.PromptForPrereleaseSuffix( version );

            builder.DownloadRock( version );

            await builder.BuildProjectAsync( "Rock.Enums" );
        }
        finally
        {
            builder.Cleanup();
        }
    }
}
