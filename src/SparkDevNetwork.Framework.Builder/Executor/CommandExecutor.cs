using System.Diagnostics;

namespace SparkDevNetwork.Framework.Builder.Executor;

/// <summary>
/// Executes a command and reports the output to a progress reporter.
/// </summary>
class CommandExecutor
{
    #region Fields

    /// <summary>
    /// The name of the command to execute.
    /// </summary>
    private readonly string _command;

    /// <summary>
    /// The arguments to pass to the command.
    /// </summary>
    private readonly string[] _arguments;

    /// <summary>
    /// The working directory to launch the command in.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// The object that will receive progress updates from the command.
    /// </summary>
    public ICommandProgress? ProgressReporter { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of <see cref="CommandExecutor"/>. 
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="args">The arguments to pass to the command.</param>
    public CommandExecutor( string command, params string[] args )
    {
        _command = command;
        _arguments = args;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Executes the command and returns the exit code.
    /// </summary>
    /// <returns>The status code from the executed command.</returns>
    public async Task<CommandResult> ExecuteAsync()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _command,
            WorkingDirectory = WorkingDirectory ?? string.Empty,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach ( var arg in _arguments )
        {
            startInfo.ArgumentList.Add( arg );
        }

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };
        var output = new List<string>();
        var tcs = new TaskCompletionSource();

        void OutputReceived( string? line )
        {
            if ( line != null )
            {
                output.Add( line );
                ProgressReporter?.OnProgress( line );
            }
        }

        void KillProcess( object? sender, ConsoleCancelEventArgs e )
        {
            try
            {
                process.Kill( true );
            }
            catch
            {
                // Ignore exceptions when killing the process.
            }
        }

        process.OutputDataReceived += ( s, ea ) => OutputReceived( ea.Data );
        process.ErrorDataReceived += ( s, ea ) => OutputReceived( ea.Data );
        process.Exited += ( s, ea ) => tcs.TrySetResult();

        Console.CancelKeyPress += KillProcess;
        ProgressReporter?.OnStarted();

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await tcs.Task;

            return new CommandResult( process.ExitCode, [.. output] );
        }
        finally
        {
            Console.CancelKeyPress -= KillProcess;
            ProgressReporter?.OnCompleted();
        }
    }

    #endregion
}
