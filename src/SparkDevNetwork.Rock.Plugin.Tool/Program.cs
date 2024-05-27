using System.CommandLine;
using System.IO.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool;

public class Program
{
    static async Task<int> Main( string[] args )
    {
        var services = new ServiceCollection();

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton( AnsiConsole.Console );
        services.AddSingleton<ILoggerFactory, DynamicLoggerFactory>();
        services.AddSingleton( ServiceDescriptor.Singleton( typeof( ILogger<> ), typeof( Logger<> ) ) );

        var serviceProvider = services.BuildServiceProvider();
        var rootCommand = ActivatorUtilities.CreateInstance<RootAppCommand>( serviceProvider );

        return await rootCommand.InvokeAsync( args );
    }
}
