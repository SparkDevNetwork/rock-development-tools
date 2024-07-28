using System.Diagnostics;
using System.IO.Abstractions;

using LibGit2Sharp;

using Microsoft.Extensions.Logging;

using SparkDevNetwork.Rock.DevTool.Data;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.DevEnvironment;

class PluginInstallation
{
    #region Fields

    /// <summary>
    /// The directory that contains the environment.
    /// </summary>
    private readonly string _pluginPath;

    /// <summary>
    /// The data that represents the plugin configuration.
    /// </summary>
    private readonly PluginReferenceData _data;

    /// <summary>
    /// The object that provides access to the file system.
    /// </summary>
    private readonly IFileSystem _fs;

    /// <summary>
    /// The logger to use when writing diagnostic messages.
    /// </summary>
    private readonly ILogger _logger;

    #endregion

    #region Properties

    /// <inheritdoc cref="PluginReferenceData.Path"/>
    public string Path => _data.Path;

    #endregion

    /// <summary>
    /// Creates a new instance of a plugin installation in the environment.
    /// </summary>
    /// <param name="pluginPath">The absolute path to the plugin.</param>
    /// <param name="data">The data that describes the plugin.</param>
    /// <param name="fileSystem">The object that will provide access to the file system.</param>
    /// <param name="logger">The object that will log diagnostic information.</param>
    public PluginInstallation( string pluginPath, PluginReferenceData data, IFileSystem fs, ILogger logger )
    {
        _pluginPath = pluginPath;
        _data = data;
        _fs = fs;
        _logger = logger;
    }

    /// <summary>
    /// Installs or updates a plugin. If the plugin is not yet installed then
    /// it will be installed. Otherwise it will be updated.
    /// </summary>
    /// <param name="context">The context used to report progress.</param>
    public void InstallOrUpdatePlugin( ProgressContext context )
    {
        if ( !_fs.Directory.Exists( _pluginPath ) || !Repository.IsValid( _pluginPath ) )
        {
            var progress = context.AddTask( $"Installing {_data.Path}", true, 1 );
            InstallPluginAsync( progress );
        }
        else
        {
            var progress = context.AddTask( $"Updating {_data.Path}", true, 1 );
            UpdatePluginAsync( progress );
        }
    }

    /// <summary>
    /// Installs the plugin into the environment.
    /// </summary>
    /// <param name="progress">The progress reporter.</param>
    private void InstallPluginAsync( IProgress<double>? progress )
    {
        if ( string.IsNullOrWhiteSpace( _data.Url ) || string.IsNullOrWhiteSpace( _data.Branch ) )
        {
            throw new InvalidOperationException( "Can't install plugin without repository url and branch name." );
        }

        Clone( _data.Url,
            _pluginPath,
            _data.Branch,
            progress );
    }

    /// <summary>
    /// Update the plugin by ensuring it is on the correct branch and also
    /// pulls any changes from the remote.
    /// </summary>
    /// <param name="progress">An optional progress reporter.</param>
    private void UpdatePluginAsync( IProgress<double>? progress )
    {
        if ( string.IsNullOrWhiteSpace( _data.Url ) || string.IsNullOrWhiteSpace( _data.Branch ) )
        {
            throw new InvalidOperationException( "Can't install plugin without repository url and branch name." );
        }

        var repo = new Repository( _pluginPath );
        var signature = repo.Config.BuildSignature( DateTimeOffset.Now );
        var currentBranch = GetCurrentBranch( repo );

        if ( currentBranch != _data.Branch )
        {
            LibGit2Sharp.Commands.Checkout( repo, _data.Branch );
        }

        var pullOptions = new PullOptions
        {
            FetchOptions = new FetchOptions
            {
                CredentialsProvider = GetCredentials,
                OnTransferProgress = ( transferProgress ) =>
                {
                    progress?.Report( transferProgress.ReceivedObjects / ( double ) transferProgress.TotalObjects );
                    return true;
                }
            },
            MergeOptions = new MergeOptions
            {
                FailOnConflict = true,
                FastForwardStrategy = FastForwardStrategy.FastForwardOnly
            }
        };

        LibGit2Sharp.Commands.Pull( repo, signature, pullOptions );

        progress?.Report( 1 );
    }

    /// <summary>
    /// Checks if the plugin is up to date with the environment configuration.
    /// </summary>
    /// <returns>An instance of <see cref="EnvironmentStatusItem"/> that describes the status.</returns>
    public PluginStatusItem GetStatus()
    {
        if ( string.IsNullOrWhiteSpace(_data.Url ) || string.IsNullOrWhiteSpace( _data.Branch ) )
        {
            return new PluginStatusItem( _data.Path, _data );
        }

        if ( !Repository.IsValid( _pluginPath ) )
        {
            _logger.LogError( "Plugin {path} is not a git repository.", _data.Path );
            return new PluginStatusItem( _data.Path, "is not a git repository.", _data );
        }

        var repository = new Repository( _pluginPath );
        var currentBranch = GetCurrentBranch( repository );

        if ( currentBranch == null )
        {
            _logger.LogInformation( "Plugin {path} is not on a branch.", _data.Path );
            return new PluginStatusItem( _data.Path, "is not on a branch.", _data );
        }

        if ( _data.Branch != currentBranch )
        {
            _logger.LogInformation( "Plugin {path} is on branch {repoBranch} instead of {expectedBranch}.", _data.Path, currentBranch, _data.Branch );
            return new PluginStatusItem( _data.Path, $"is on branch {currentBranch} but should be {_data.Branch}.", _data );
        }

        var remote = repository.Network.Remotes[repository.Head.RemoteName];
        var refSpecs = remote.FetchRefSpecs.Select( r => r.Specification );

        if ( !repository.Head.TrackingDetails.BehindBy.HasValue )
        {
            return new PluginStatusItem( _data.Path, "has no upstream remote configured.", _data );
        }

        LibGit2Sharp.Commands.Fetch( repository, remote.Name, refSpecs, new FetchOptions
        {
            CredentialsProvider = GetCredentials,
        }, "Fetching remote" );

        if ( repository.Head.TrackingDetails.BehindBy.Value > 0 )
        {
            return new PluginStatusItem( _data.Path, $"is behind by {repository.Head.TrackingDetails.BehindBy} commits.", _data );
        }

        return new PluginStatusItem( _data.Path, _data );
    }

    /// <summary>
    /// Checks if the plugin is clean. A clean installation means that the git
    /// repository is in a clean state.
    /// </summary>
    /// <returns><c>true</c> if the plugin is in a clean state; otherwise <c>false</c>.</returns>
    public bool IsClean()
    {
        // If the directory does not exist, it is considered clean so that
        // an update command can execute.
        if ( !_fs.Directory.Exists( _pluginPath ) )
        {
            return true;
        }

        // If the directory exists but is empty iti s considered clean.
        if ( _fs.Directory.GetFiles( _pluginPath ).Length == 0 && _fs.Directory.GetDirectories( _pluginPath ).Length == 0 )
        {
            return true;
        }

        if ( !Repository.IsValid( _pluginPath ) )
        {
            _logger.LogError( "Plugin {path} is not a git repository.", _data.Path );
            return false;
        }

        using var repository = new Repository( _pluginPath );

        return !repository.RetrieveStatus().IsDirty;
    }

    /// <summary>
    /// Clones a remote repository into the environment.
    /// </summary>
    /// <param name="remoteUrl">The URL of the remote repository.</param>
    /// <param name="destinationDirectory">The path to the directory to clone the repository into.</param>
    /// <param name="branch">If specified the name of the remote branch to clone; otherwise the default branch will be cloned.</param>
    /// <param name="progress">An optional progress reporter for the clone progress.</param>
    private static void Clone( string remoteUrl, string destinationDirectory, string? branch, IProgress<double>? progress )
    {
        Repository.Clone( remoteUrl, destinationDirectory, new CloneOptions
        {
            BranchName = !string.IsNullOrEmpty( branch ) ? branch : null,
            OnCheckoutProgress = ( a, b, c ) =>
            {
                progress?.Report( 0.5 + ( b / ( double ) c / 2.0 ) );
            },
            FetchOptions =
            {
                CredentialsProvider = GetCredentials,
                OnTransferProgress = ( transferProgress ) =>
                {
                    progress?.Report( transferProgress.ReceivedObjects / ( double ) transferProgress.TotalObjects / 2.0 );
                    return true;
                }
            }
        } );
    }

    /// <summary>
    /// Gets the credentials from the native git implementation for the repository.
    /// </summary>
    /// <param name="repoUrl">The URL of the repository that needs authentication.</param>
    /// <param name="usernameFromUrl">The username to get credentials for.</param>
    /// <param name="supportedTypes">The supported authentication types.</param>
    /// <returns>A set of credentials to authenticate with.</returns>
    /// <exception cref="NoCredentialsException">Thrown if no credentials are available.</exception>
    private static UsernamePasswordCredentials GetCredentials( string repoUrl, string usernameFromUrl, SupportedCredentialTypes supportedTypes )
    {
        var uri = new Uri( repoUrl );
        string? username = null;
        string? password = null;

        try
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git.exe",
                    Arguments = "credential-manager get",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();

            proc.StandardInput.WriteLine( $"protocol={uri.Scheme}" );
            proc.StandardInput.WriteLine( $"host={uri.Host}" );

            if ( !string.IsNullOrEmpty( usernameFromUrl ) )
            {
                proc.StandardInput.WriteLine( $"username={usernameFromUrl}" );
            }

            proc.StandardInput.WriteLine();

            while ( !proc.StandardOutput.EndOfStream )
            {
                var line = proc.StandardOutput.ReadLine();

                if ( line?.StartsWith( "username=" ) == true )
                {
                    username = line.Substring( 9 );
                }
                else if ( line?.StartsWith( "password=" ) == true )
                {
                    password = line.Substring( 9 );
                }
            }

            proc.WaitForExit();
        }
        catch
        {
            username = null;
            password = null;
        }

        if ( username != null && password != null )
        {
            return new UsernamePasswordCredentials
            {
                Username = username,
                Password = password
            };
        }

        throw new NoCredentialsException();
    }

    /// <summary>
    /// Gets the current branch name of the repository.
    /// </summary>
    /// <param name="repository">The repository.</param>
    /// <returns>The name of the branch or <c>null</c> if not on any branch.</returns>
    private static string? GetCurrentBranch( Repository repository )
    {
        var reference = repository.Head.Reference.TargetIdentifier;

        if ( !reference.StartsWith( "refs/heads/" ) )
        {
            return null;
        }

        return reference.Substring( 11 );
    }
}
