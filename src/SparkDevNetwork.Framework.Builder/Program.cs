namespace SparkDevNetwork.Framework.Builder;

class Program
{
    static void Main( string[] args )
    {
        var builder = new RockBuilder();

        var version = builder.PromptForRockVersion();
        var suffix = builder.PromptForPrereleaseSuffix( version );

        var rockPath = Directory.CreateTempSubdirectory().FullName;

        try
        {
            Console.WriteLine( $"Downloading to {rockPath}" );

            // builder.DownloadRock( version, rockPath );
        }
        finally
        {
            RockBuilder.DeleteRepository( rockPath );
            Console.WriteLine( $"Deleted {rockPath}" );
        }
    }
}
