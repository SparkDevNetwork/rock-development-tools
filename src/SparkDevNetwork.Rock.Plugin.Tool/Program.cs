using System.CommandLine;

namespace SparkDevNetwork.Rock.Plugin.Tool;

public class Program
{
    static async Task<int> Main( string[] args )
    {
        return await new RootAppCommand().InvokeAsync( args );
    }
}
