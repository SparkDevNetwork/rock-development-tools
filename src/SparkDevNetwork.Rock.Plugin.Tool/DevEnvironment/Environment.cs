using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.Json;

using LibGit2Sharp;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SparkDevNetwork.Rock.Plugin.Tool.Data;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.DevEnvironment;

/// <summary>
/// Handles updating and installing environments.
/// </summary>
class Environment
{
    #region Fields

    /// <summary>
    /// The directory that contains the environment.
    /// </summary>
    private readonly string _environmentDirectory;

    /// <summary>
    /// The data that represents the environment configuration.
    /// </summary>
    private readonly EnvironmentData _data;

    /// <summary>
    /// The console to use when writing messages to standard output.
    /// </summary>
    private readonly IAnsiConsole _console;

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

    /// <summary>
    /// Determines if this operation is a dry-run. When <c>true</c> no changes
    /// will be made to files on disk.
    /// </summary>
    public bool IsDryRun { get; set; }

    #endregion

    /// <summary>
    /// Creates a new instance of the environment helper.
    /// </summary>
    /// <param name="logger">The logger to use when writing diagnostic messages.</param>
    private Environment( string environmentDirectory, EnvironmentData data, IServiceProvider serviceProvider )
    {
        _environmentDirectory = environmentDirectory;
        _data = data;
        _console = serviceProvider.GetRequiredService<IAnsiConsole>();
        _fs = serviceProvider.GetRequiredService<IFileSystem>();
        _logger = serviceProvider.GetRequiredService<ILogger>();
    }

    /// <summary>
    /// Opens an existing environment from a directory.
    /// </summary>
    /// <param name="environmentDirectory">The directory that contains the environment.</param>
    /// <param name="console">The console object to use when writing console messages.</param>
    /// <param name="loggerFactory">The factory to create new logging facilities.</param>
    /// <returns>An instance of <see cref="Environment"/> or <c>null</c> if it could not be opened.</returns>
    public static Environment Open( string environmentDirectory, IServiceProvider serviceProvider )
    {
        var fs = serviceProvider.GetRequiredService<IFileSystem>();
        var environmentFile = fs.Path.Combine( environmentDirectory, EnvironmentData.Filename );

        if ( !fs.File.Exists( environmentFile ) )
        {
            throw new InvalidEnvironmentException( $"No environment file was found at {environmentFile}." );
        }

        var json = fs.File.ReadAllText( environmentFile );
        var data = JsonSerializer.Deserialize<EnvironmentData>( json )
            ?? throw new InvalidEnvironmentException( $"Invalid environment configuration found in {environmentFile}." );

        // Validate all plugins and abort if any plugin is not valid.
        foreach ( var plugin in data.Plugins )
        {
            if ( string.IsNullOrWhiteSpace( plugin.Path ) )
            {
                throw new InvalidEnvironmentException( "One or more plugins were defined without a path, all plugins must define a path." );
            }

            if ( string.IsNullOrWhiteSpace( plugin.Url ) )
            {
                throw new InvalidEnvironmentException( $"No url defined for plugin {plugin.Path}." );
            }

            if ( string.IsNullOrWhiteSpace( plugin.Branch ) )
            {
                throw new InvalidEnvironmentException( $"No branch defined for plugin {plugin.Path}." );
            }
        }

        return new Environment( environmentDirectory, data, serviceProvider );
    }

    /// <summary>
    /// Gets a new object instance that will handle the Rock installation.
    /// </summary>
    /// <returns>A new instance of <see cref="RockInstallation"/>.</returns>
    public RockInstallation GetRockInstallation()
    {
        var rockPath = _fs.Path.Combine( _environmentDirectory, "Rock" );
        var rock = new RockInstallation( rockPath, _data.Rock, _fs, _console, _logger )
        {
            IsDryRun = IsDryRun
        };

        return rock;
    }

    /// <summary>
    /// Gets all the plugins defined in this environment.
    /// </summary>
    /// <returns>A list of plugins.</returns>
    public List<PluginData> GetPlugins()
    {
        return _data.Plugins;
    }

    /// <summary>
    /// Installs or updates a plugin. If the plugin is not yet installed then
    /// it will be installed. Otherwise it will be updated.
    /// </summary>
    /// <param name="plugin">The plugin to be installed or updated.</param>
    /// <param name="context">The context used to report progress.</param>
    public void InstallOrUpdatePlugin( PluginData plugin, ProgressContext context )
    {
        var pluginPath = _fs.Path.Combine( _environmentDirectory, plugin.Path );

        if ( !_fs.Directory.Exists( pluginPath ) || !Repository.IsValid( pluginPath ) )
        {
            var progress = context.AddTask( $"Installing {plugin.Path}", true, 1 );
            InstallPluginAsync( plugin, progress );
        }
        else
        {
            var progress = context.AddTask( $"Updating {plugin.Path}", true, 1 );
            UpdatePluginAsync( plugin, progress );
        }
    }

    /// <summary>
    /// Installs the plugin into the environment.
    /// </summary>
    /// <param name="plugin">The plugin to be installed.</param>
    /// <param name="progress">The progress reporter.</param>
    private void InstallPluginAsync( PluginData plugin, IProgress<double>? progress )
    {
        var pluginPath = _fs.Path.Combine( _environmentDirectory, plugin.Path );

        Clone( plugin.Url,
            pluginPath,
            plugin.Branch,
            progress );
    }

    /// <summary>
    /// Update the plugin by ensuring it is on the correct branch and also
    /// pulls any changes from the remote.
    /// </summary>
    /// <param name="plugin">The plugin to be updated.</param>
    /// <param name="progress">An optional progress reporter.</param>
    private void UpdatePluginAsync( PluginData plugin, IProgress<double>? progress )
    {
        var pluginPath = _fs.Path.Combine( _environmentDirectory, plugin.Path );
        var repo = new Repository( pluginPath );
        var signature = repo.Config.BuildSignature( DateTimeOffset.Now );
        var currentBranch = GetCurrentBranch( repo );

        if ( currentBranch != plugin.Branch )
        {
            LibGit2Sharp.Commands.Checkout( repo, plugin.Branch );
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
    /// Gets the status of the environment by way of individual status
    /// items to describe the status of Rock and each plugin.
    /// </summary>
    /// <returns>A list of status items.</returns>
    public List<StatusItem> GetEnvironmentStatus()
    {
        var statuses = new List<StatusItem>
        {
            GetRockInstallation().GetRockStatus()
        };

        foreach ( var plugin in _data.Plugins )
        {
            statuses.Add( GetPluginStatus( plugin ) );
        }

        return statuses;
    }

    /// <summary>
    /// Checks if the environment is up to date with the configuration.
    /// </summary>
    /// <returns><c>true</c> if the environment is already up to date; otherwise <c>false</c>.</returns>
    public bool IsEnvironmentUpToDate()
    {
        return GetEnvironmentStatus().All( s => s.IsUpToDate );
    }

    /// <summary>
    /// Checks if the plugin is up to date with the environment configuration.
    /// </summary>
    /// <param name="plugin">The plugin configuration.</param>
    /// <returns>An instance of <see cref="EnvironmentStatusItem"/> that describes the status.</returns>
    public PluginStatusItem GetPluginStatus( PluginData plugin )
    {
        var pluginDirectory = _fs.Path.Combine( _environmentDirectory, plugin.Path.Replace( '/', Path.PathSeparator ) );

        if ( !Repository.IsValid( pluginDirectory ) )
        {
            _logger.LogError( "Plugin {path} is not a git repository.", plugin.Path );
            return new PluginStatusItem( plugin.Path, "is not a git repository.", plugin );
        }

        var repository = new Repository( pluginDirectory );
        var currentBranch = GetCurrentBranch( repository );

        if ( currentBranch == null )
        {
            _logger.LogInformation( "Plugin {path} is not on a branch.", plugin.Path );
            return new PluginStatusItem( plugin.Path, "is not on a branch.", plugin );
        }

        if ( plugin.Branch != currentBranch )
        {
            _logger.LogInformation( "Plugin {path} is on branch {repoBranch} instead of {expectedBranch}.", plugin.Path, currentBranch, plugin.Branch );
            return new PluginStatusItem( plugin.Path, $"is on branch {currentBranch} but should be {plugin.Branch}.", plugin );
        }

        var remote = repository.Network.Remotes[repository.Head.RemoteName];
        var refSpecs = remote.FetchRefSpecs.Select( r => r.Specification );

        if ( !repository.Head.TrackingDetails.BehindBy.HasValue )
        {
            return new PluginStatusItem( plugin.Path, "has no upstream remote configured.", plugin );
        }

        LibGit2Sharp.Commands.Fetch( repository, remote.Name, refSpecs, new FetchOptions
        {
            CredentialsProvider = GetCredentials,
        }, "Fetching remote" );

        if ( repository.Head.TrackingDetails.BehindBy.Value > 0 )
        {
            return new PluginStatusItem( plugin.Path, $"is behind by {repository.Head.TrackingDetails.BehindBy} commits.", plugin );
        }

        return new PluginStatusItem( plugin.Path, plugin );
    }

    /// <summary>
    /// Checks if the plugin is clean. A clean installation means that the git
    /// repository is in a clean state.
    /// </summary>
    /// <returns><c>true</c> if the plugin is in a clean state; otherwise <c>false</c>.</returns>
    public bool IsPluginClean( PluginData plugin )
    {
        var pluginDirectory = _fs.Path.Combine( _environmentDirectory, plugin.Path.Replace( '/', Path.PathSeparator ) );

        // If the directory does not exist, it is considered clean so that
        // an update command can execute.
        if ( !_fs.Directory.Exists( pluginDirectory ) )
        {
            return true;
        }

        // If the directory exists but is empty iti s considered clean.
        if ( _fs.Directory.GetFiles( pluginDirectory ).Length == 0 && _fs.Directory.GetDirectories( pluginDirectory ).Length == 0 )
        {
            return true;
        }

        if ( !Repository.IsValid( pluginDirectory ) )
        {
            _logger.LogError( "Plugin {path} is not a git repository.", plugin.Path );
            return false;
        }

        using var repository = new Repository( pluginDirectory );

        return !repository.RetrieveStatus().IsDirty;
    }

    /// <summary>
    /// Clones a remote repository into the environment.
    /// </summary>
    /// <param name="remoteUrl">The URL of the remote repository.</param>
    /// <param name="relativeDirectory">The relative path to the environment root.</param>
    /// <param name="branch">If specified the name of the remote branch to clone; otherwise the default branch will be cloned.</param>
    /// <param name="progress">An optional progress reporter for the clone progress.</param>
    private void Clone( string remoteUrl, string relativeDirectory, string? branch, IProgress<double>? progress )
    {
        var destinationDirectory = _fs.Path.Combine( _environmentDirectory, relativeDirectory );

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
