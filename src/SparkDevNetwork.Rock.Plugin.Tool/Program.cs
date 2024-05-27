using System.CommandLine;
using System.IO.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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

class DynamicLoggerFactory : ILoggerFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public bool IsEnabled { get; set; }

    public DynamicLoggerFactory()
    {
        _loggerFactory = LoggerFactory.Create( config =>
        {
            config.AddSimpleConsole( options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "[HH:mm:ss.fff] ";
            } );

            config.SetMinimumLevel( LogLevel.Information );
        } );
    }

    public void AddProvider( ILoggerProvider provider )
    {
        _loggerFactory.AddProvider( provider );
    }

    public ILogger CreateLogger( string categoryName )
    {
        if ( !IsEnabled )
        {
            return NullLogger.Instance;
        }

        return _loggerFactory.CreateLogger( categoryName );
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
    }
}
