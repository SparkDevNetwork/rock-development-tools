using Microsoft.Extensions.Logging;

namespace SparkDevNetwork.Rock.Plugin.Tool;

public class Program
{
    static async Task<int> Main( string[] args )
    {
        // var fruit = AnsiConsole.Prompt(
        //     new SelectionPrompt<string>()
        //         .Title( "What's your [green]favorite fruit[/]?" )
        //         .PageSize( 10 )
        //         .MoreChoicesText( "[grey](Move up and down to reveal more fruits)[/]" )
        //         .AddChoices( new[] {
        //     "Apple", "Apricot", "Avocado",
        //     "Banana", "Blackcurrant", "Blueberry",
        //     "Cherry", "Cloudberry", "Cocunut",
        //         } ) );

        // return 0;
        // var loggerFactory = LoggerFactory.Create( cfg =>
        // {
        //     cfg.AddSimpleConsole( options =>
        //     {
        //         options.SingleLine = true;
        //         options.TimestampFormat = "[HH:mm:ss.fff] ";
        //     } );
        // } );

        // //var logger = sp.GetRequiredService<ILogger<Program>>();
        // var logger = loggerFactory.CreateLogger( nameof(Program));

        // logger.LogInformation( "hi3" );
        // logger.LogDebug( "hi4" );
        // return 0;

        return await new RootAppCommand().InvokeAsync( args );
    }
}
