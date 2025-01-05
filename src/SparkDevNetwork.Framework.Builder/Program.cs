namespace SparkDevNetwork.Framework.Builder;

class Program
{
    static async Task Main( string[] args )
    {
        // var refs = RockBuilder.GetRockVersions();

        // foreach ( var r in refs )
        // {
        //     Console.WriteLine( r.Version );
        // }

        await UI.ProgressBar.Run( "Processing", 25, async bar =>
        {
            for ( int i = 0; i < 25; i++ )
            {
                bar.SetStep( i );
                await Task.Delay( 100 );
            }

            return true;
        } );

        await UI.IndeterminateBar.Run( "Thinking", async bar =>
        {
            await Task.Delay( 5000 );
            return true;
        } );
    }
}
