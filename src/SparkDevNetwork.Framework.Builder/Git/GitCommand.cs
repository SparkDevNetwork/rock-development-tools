using System.Text.RegularExpressions;

using SparkDevNetwork.Framework.Builder.Executor;

namespace SparkDevNetwork.Framework.Builder.Git;

/// <summary>
/// Provides access to git commands that will need to be executed at various
/// times during the building process.
/// </summary>
static partial class GitCommand
{
    /// <summary>
    /// The regular expression that will match git refs send to stdout.
    /// </summary>
    [GeneratedRegex( @"^([0-9a-fA-F]+)\s+(.+)$" )]
    private static partial Regex ListRemoteRefLineRegExp();

    /// <summary>
    /// Lists git references in a remote repository.
    /// </summary>
    /// <param name="remoteRepo">The URL of the remote repository.</param>
    /// <param name="refSpecs">The ref specifications that will be retrieved.</param>
    /// <returns>A collection of <see cref="GitReference"/> objects.</returns>
    public static List<GitReference> ListRemoteReferences( string remoteRepo, params string[] refSpecs )
    {
        var reporter = new StringsCommandProgress();
        var executor = new CommandExecutor( "git", ["ls-remote", remoteRepo, .. refSpecs] )
        {
            ProgressReporter = reporter
        };

        if ( executor.Execute() != 0 )
        {
            throw new Exception( "Failed to list remote refs." );
        }

        var references = new List<GitReference>();

        foreach ( var msg in reporter.Output )
        {
            var match = ListRemoteRefLineRegExp().Match( msg );

            if ( match.Success )
            {
                references.Add( new GitReference
                {
                    CommitHash = match.Groups[1].Value,
                    Ref = match.Groups[2].Value
                } );
            }
        }

        return references;
    }
}
