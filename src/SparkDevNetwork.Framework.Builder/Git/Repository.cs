using System.Text.RegularExpressions;

using LibGit2Sharp;

using Semver;

namespace SparkDevNetwork.Framework.Builder.Git;

class Repository
{
    /// <summary>
    /// The characters used for new line in strings. This is used to split
    /// strings from the server because it will sometimes report multiple
    /// messages in a multiple lines at once.
    /// </summary>
    private static readonly char[] newLineChars = ['\r', '\n'];

    /// <summary>
    /// Clones the remote repository into the local path. If the local path
    /// already exists then an exception will be raised.
    /// </summary>
    /// <param name="remoteRepo">The remote repository URL.</param>
    /// <param name="localPath">The local path to clone the repository into.</param>
    /// <param name="reporter">The optional progress reporter.</param>
    /// <returns>A <see cref="Task"/> that represents the operation.</returns>
    public static Task CloneAsync( string remoteRepo, string localPath, string? branchName, IGitProgressReporter? reporter )
    {
        var cloneOptions = new CloneOptions
        {
            BranchName = branchName,
            // Progress handler for checking out files once the repo is cloned.
            OnCheckoutProgress = ( string path, int completedSteps, int totalSteps ) =>
            {
                reporter?.OnProgress( "Checking out", $"File {completedSteps}/{totalSteps}" );
            }
        };

        // Progress handler for network transfer.
        cloneOptions.FetchOptions.OnTransferProgress = transferProgress =>
        {
            var bytes = transferProgress.ReceivedBytes > 1024 * 1024
                ? $"{transferProgress.ReceivedBytes / 1024.0 / 1024.0:N2}MB"
                : $"{transferProgress.ReceivedBytes / 1024.0:N0}KB";

            reporter?.OnProgress( "Transfering objects", $"{transferProgress.ReceivedObjects}/{transferProgress.TotalObjects}, {bytes}" );

            return true;
        };

        // Progress handler for messages from the server.
        cloneOptions.FetchOptions.OnProgress = serverOutput =>
        {
            var message = serverOutput.Split( newLineChars, StringSplitOptions.RemoveEmptyEntries )
                .LastOrDefault()
                ?.Split( ":", 2 );

            if ( message?.Length == 2 )
            {
                reporter?.OnProgress( message[0], message[1].Trim() );
            }

            return true;
        };

        return Task.Run( () =>
        {
            reporter?.OnStarted();
            LibGit2Sharp.Repository.Clone( remoteRepo, localPath, cloneOptions );
            reporter?.OnCompleted();
        } );
    }

    /// <summary>
    /// Deletes the local repository at the given path. This shouldbe used
    /// instead of Directory.Delete() because the .git folder has files with
    /// special file attributes that prevent normal deletion.
    /// </summary>
    /// <param name="localPath">The local repository path to delete.</param>
    public static void DeleteRepository( string localPath )
    {
        if ( !Directory.Exists( localPath ) )
        {
            return;
        }

        foreach ( string subdirectory in Directory.EnumerateDirectories( localPath ) )
        {
            DeleteRepository( subdirectory );
        }

        foreach ( string fileName in Directory.EnumerateFiles( localPath ) )
        {
            var fileInfo = new FileInfo( fileName )
            {
                Attributes = FileAttributes.Normal
            };

            fileInfo.Delete();
        }

        Directory.Delete( localPath );
    }

    public static IEnumerable<string> GetRockVersions()
    {
        var versionRegExp = new Regex( @"refs\/tags\/((\d+)\.(\d+)\.(\d+)([\.-].*)?)", RegexOptions.IgnoreCase );
        var minimumVersion = new SemVersion( 1, 16, 0 );

        var items = LibGit2Sharp.Repository
            .ListRemoteReferences( "https://github.com/SparkDevNetwork/Rock" )
            .Where( r => r.IsTag )
            .Select( r => new
            {
                Match = versionRegExp.Match( r.CanonicalName ),
                Commit = r.TargetIdentifier
            } )
            .Where( r => r.Match.Success )
            .Select( r => new
            {
                Tag = r.Match.Groups[1].Value,
                Major = int.Parse( r.Match.Groups[2].Value ),
                Minor = int.Parse( r.Match.Groups[3].Value ),
                Patch = int.Parse( r.Match.Groups[4].Value ),
                Prerelease = ParsePrereleaseSuffix( r.Match.Groups[5].Value ),
                r.Commit
            } )
            .Select( v => new
            {
                Version = new SemVersion( v.Major, v.Minor, v.Patch, v.Prerelease ),
                v.Tag,
                v.Commit
            } )
            .Where( v => v.Version.ComparePrecedenceTo( minimumVersion ) >= 0 )
            .ToList();

        return [""];
    }

    private static IEnumerable<string> ParsePrereleaseSuffix( string suffix )
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
}
