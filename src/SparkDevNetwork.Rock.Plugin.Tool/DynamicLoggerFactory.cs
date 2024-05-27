using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SparkDevNetwork.Rock.Plugin.Tool;

/// <summary>
/// A custom logger factory that only returns real loggers if logging has
/// been enabled.
/// </summary>
class DynamicLoggerFactory : ILoggerFactory
{
    /// <summary>
    /// The actual logger factory that will be used.
    /// </summary>
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// <c>true</c> if logging is enabled for this application instance.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="DynamicLoggerFactory"/>.
    /// </summary>
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

    /// <inheritdoc/>
    public void AddProvider( ILoggerProvider provider )
    {
        _loggerFactory.AddProvider( provider );
    }

    /// <inheritdoc/>
    public ILogger CreateLogger( string categoryName )
    {
        if ( !IsEnabled )
        {
            return NullLogger.Instance;
        }

        return _loggerFactory.CreateLogger( categoryName );
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _loggerFactory.Dispose();
    }
}
