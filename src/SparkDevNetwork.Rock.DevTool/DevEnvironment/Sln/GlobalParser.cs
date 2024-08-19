namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// A parser for the Global group in a solution file.
/// </summary>
class GlobalParser : ISolutionItemParser
{
    /// <inheritdoc/>
    public bool Parse( string line, SlnReader reader )
    {
        if ( !line.Equals( "Global", StringComparison.OrdinalIgnoreCase ) )
        {
            return false;
        }

        reader.ParseBlock( "EndGlobal", reader.Solution.Global );

        return true;
    }
}
