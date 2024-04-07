using SparkDevNetwork.Framework.Builder.Git;

namespace SparkDevNetwork.Framework.Builder;

class Program
{
    static async Task Main( string[] args )
    {
        var refs = Repository.GetRockVersions();
        var repoDir = Path.Combine( Directory.GetCurrentDirectory(), "language-tools" );

        Repository.DeleteRepository( repoDir );

        await Repository.CloneAsync( "https://github.com/vuejs/language-tools", repoDir, null, new ConsoleGitProgressReporter() );

        var repo = new LibGit2Sharp.Repository( repoDir );
        repo.RevParse( "HEAD", out var reference, out _ );
        Console.WriteLine( $"Commit: {reference.TargetIdentifier}" );
    }
}
