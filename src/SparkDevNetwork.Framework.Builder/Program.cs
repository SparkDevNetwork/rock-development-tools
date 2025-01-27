using System.CommandLine;

namespace SparkDevNetwork.Framework.Builder;

/// <summary>
/// The main entry point for the application.
/// </summary>
class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">The arguments passed on the command line.</param>
    /// <returns>The exit status, 0 for success.</returns>
    static async Task<int> Main( string[] args )
    {
        var rootCommand = new RootAppCommand();

        return await rootCommand.InvokeAsync( args );
    }
}
