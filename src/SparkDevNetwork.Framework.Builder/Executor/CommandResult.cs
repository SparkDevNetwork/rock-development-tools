namespace SparkDevNetwork.Framework.Builder.Executor;

/// <summary>
/// The result of an executed command.
/// </summary>
class CommandResult
{
    /// <summary>
    /// The exit code returned by the command.
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// The output to stdout and stderr from the command.
    /// </summary>
    public string[] Output { get; }

    /// <summary>
    /// Creates a new instance of <see cref="CommandResult"/>. 
    /// </summary>
    /// <param name="exitCode">The exit code returned by the command.</param>
    /// <param name="output">The output to stdout and stderr from the command.</param>
    public CommandResult( int exitCode, string[]? output = null )
    {
        ExitCode = exitCode;
        Output = output ?? [];
    }

    /// <summary>
    /// Writes the buffered output to the console.
    /// </summary>
    public void WriteOutput()
    {
        foreach ( var line in Output )
        {
            Console.WriteLine( line );
        }
    }
}
