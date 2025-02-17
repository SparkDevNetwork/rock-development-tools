using System.CommandLine;
using System.IO.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool;

public class Program
{
    static async Task<int> Main( string[] args )
    {
        await VersionChecker.CheckToolVersionAsync();

        var services = new ServiceCollection();

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton( AnsiConsole.Console );
        services.AddSingleton<ILoggerFactory, DynamicLoggerFactory>();
        services.AddSingleton( sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger( "RockTool" ) );
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var rootCommand = ActivatorUtilities.CreateInstance<RootAppCommand>( serviceProvider );

        return await rootCommand.InvokeAsync( args );
    }
}
