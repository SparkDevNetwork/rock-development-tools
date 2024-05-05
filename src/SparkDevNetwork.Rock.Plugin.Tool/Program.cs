using System.CommandLine;

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

        return await new RootAppCommand().InvokeAsync( args );
    }
}
