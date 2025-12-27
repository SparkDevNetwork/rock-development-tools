using SparkDevNetwork.Framework.Builder.Executor;

namespace SparkDevNetwork.Framework.Builder;

/// <summary>
/// Handles actions related to Visual Studio (MSBuild and NuGet).
/// </summary>
class VisualStudio
{
    #region Fields

    /// <summary>
    /// The directory to place the downloaded tools into.
    /// </summary>
    private readonly string _toolsDirectory;

    #endregion

    #region Constructors

    /// <summary>
    /// Create a new instance of <see cref="VisualStudio"/>. 
    /// </summary>
    /// <param name="toolsDirectory">The directory to place the downloaded tools into.</param>
    public VisualStudio( string toolsDirectory )
    {
        _toolsDirectory = toolsDirectory;

        if ( !Directory.Exists( toolsDirectory ) )
        {
            Directory.CreateDirectory( toolsDirectory );
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Download a tool that will be executed. If the tool already exists then
    /// no download is performed.
    /// </summary>
    /// <param name="url">The URL to download the tool from.</param>
    /// <param name="filename">The filename of the tool without any path information.</param>
    /// <returns>The full path and filename of the local tool.</returns>
    private async Task<string> DownloadToolAsync( string url, string filename )
    {
        var path = Path.Combine( _toolsDirectory, filename );

        if ( !File.Exists( path ) )
        {
            var bytes = await new HttpClient().GetByteArrayAsync( url );

            File.WriteAllBytes( path, bytes );
        }

        return path;
    }

    /// <summary>
    /// Gets the path and filename that can be used to execute msbuild.
    /// </summary>
    /// <returns>The path tot he msbuild.exe executable.</returns>
    private async Task<string> GetMSBuildPath()
    {
        var commandPath = await DownloadToolAsync( "https://github.com/microsoft/vswhere/releases/download/3.1.7/vswhere.exe", "vswhere.exe" );
        var command = new CommandExecutor( commandPath,
            "-latest",
            "-requires",
            "Microsoft.Component.MSBuild",
            "-find",
            "MSBuild\\**\\Bin\\MSBuild.exe" );

        var commandResult = await command.ExecuteAsync();

        if ( commandResult.ExitCode != 0 )
        {
            throw new Exception( "Unable to find Visual Studio installation." );
        }

        return commandResult.Output[0].Trim();
    }

    /// <summary>
    /// Executes the msbuild process with the given arguments.
    /// </summary>
    /// <param name="arguments">The arguments to pass to msbuild.</param>
    /// <param name="workingDirectory">The working directory to start the command in.</param>
    /// <returns><c>true</c> if the process was successful.</returns>
    public async Task<CommandResult> BuildAsync( string[] arguments, string workingDirectory )
    {
        var commandPath = await GetMSBuildPath();
        var command = new CommandExecutor( commandPath, arguments )
        {
            WorkingDirectory = workingDirectory
        };

        return await command.ExecuteAsync();
    }

    /// <summary>
    /// Executes the dotnet process with the given arguments.
    /// </summary>
    /// <param name="arguments">The arguments to pass to msbuild.</param>
    /// <param name="workingDirectory">The working directory to start the command in.</param>
    /// <returns><c>true</c> if the process was successful.</returns>
    public async Task<CommandResult> DotnetAsync( string[] arguments, string workingDirectory )
    {
        var commandPath = await GetMSBuildPath();
        var command = new CommandExecutor( "C:\\Program Files\\dotnet\\dotnet.exe", arguments )
        {
            WorkingDirectory = workingDirectory
        };

        return await command.ExecuteAsync();
    }

    /// <summary>
    /// Executes the nuget process with the given arguments.
    /// </summary>
    /// <param name="arguments">The arguments to pass to nuget.</param>
    /// <param name="workingDirectory">The working directory to start the process in.</param>
    /// <returns><c>true</c> if the process was successful.</returns>
    public async Task<CommandResult> NuGetAsync( string[] arguments, string workingDirectory )
    {
        var commandPath = await DownloadToolAsync( "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe", "nuget.exe" );
        var command = new CommandExecutor( commandPath, arguments )
        {
            WorkingDirectory = workingDirectory
        };

        return await command.ExecuteAsync();
    }

    /// <summary>
    /// Executes the npm process with the given arguments.
    /// </summary>
    /// <param name="arguments">The arguments to pass to nuget.</param>
    /// <param name="workingDirectory">The working directory to start the process in.</param>
    /// <returns><c>true</c> if the process was successful.</returns>
    public Task<CommandResult> Npm( string[] arguments, string workingDirectory )
    {
        var commandPath = FindExecutablePath( "npm" );

        if ( commandPath.EndsWith( ".cmd" ) )
        {
            var command = new CommandExecutor( "cmd.exe", ["/C", commandPath, .. arguments] )
            {
                WorkingDirectory = workingDirectory
            };

            return command.ExecuteAsync();
        }
        else
        {
            var command = new CommandExecutor( commandPath, arguments )
            {
                WorkingDirectory = workingDirectory
            };

            return command.ExecuteAsync();
        }
    }

    private static string FindExecutablePath( string executable )
    {
        var paths = Environment.GetEnvironmentVariable( "PATH" )?.Split( ';' );
        var extensions = new string[] { ".exe", ".com", ".cmd", "" };

        if ( paths == null )
        {
            return executable;
        }

        foreach ( var path in paths )
        {
            foreach ( var ext in extensions )
            {
                var executablePath = Path.Combine( path, $"{executable}{ext}" );

                if ( File.Exists( executablePath ) )
                {
                    return executablePath;
                }
            }
        }

        return executable;
    }

    #endregion
}
