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
    public int Execute()
    {
        ProgressReporter?.OnStarted();

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _command,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            foreach ( var arg in _arguments )
            {
                startInfo.ArgumentList.Add( arg );
            }

            var proc = new Process
            {
                StartInfo = startInfo
            };

            proc.Start();

            while ( !proc.StandardOutput.EndOfStream )
            {
                var line = proc.StandardOutput.ReadLine();

                if ( line != null )
                {
                    ProgressReporter?.OnProgress( line );
                }
            }

            proc.WaitForExit();

            return proc.ExitCode;
        }
        finally
        {
            ProgressReporter?.OnCompleted();
        }
    }

    #endregion
}
