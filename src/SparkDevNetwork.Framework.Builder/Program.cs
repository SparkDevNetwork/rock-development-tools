namespace SparkDevNetwork.Framework.Builder;

class Program
{
    static void Main( string[] args )
    {
        var rockPath = Directory.CreateTempSubdirectory().FullName;

        Console.WriteLine( $"Downloading to {rockPath}" );

        try
        {
            RockBuilder.DownloadRock( new RockVersionTag
            {
                CommitHash = "",
                Version = new Semver.SemVersion( 1, 16, 7 ),
                Tag = "1.16.7"
            }, rockPath );
        }
        finally
        {
            RockBuilder.DeleteRepository( rockPath );
        Console.WriteLine( $"Deleted {rockPath}" );
        }
    }
}
