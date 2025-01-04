using System.Text.RegularExpressions;

namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// A parser for the header data in a solution file. 
/// </summary>
partial class SolutionHeaderParser : ISolutionItemParser
{
    /// <inheritdoc/>
    public bool Parse( string line, SlnReader reader )
    {
        if ( reader.CurrentContext != null )
        {
            return false;
        }

        if ( line.StartsWith( "Microsoft Visual Studio Solution File" ) )
        {
            return true;
        }

        if ( line.StartsWith( "# Visual Studio Version" ) )
        {
            return true;
        }

        var match = VisualStudioVersionRegex().Match( line );
        if ( match.Success )
        {
            reader.Solution.Header.VisualStudioVersion = Version.Parse( match.Groups[1].Value );
            return true;
        }

        match = MinimumVisualStudioVersionRegex().Match( line );
        if ( match.Success )
        {
            reader.Solution.Header.MinimumVisualStudioVersion = Version.Parse( match.Groups[1].Value );
            return true;
        }

        return false;
    }

    [GeneratedRegex( "^VisualStudioVersion\\s*=\\s*([0-9\\.]+)", RegexOptions.IgnoreCase, "en-US" )]
    private static partial Regex VisualStudioVersionRegex();

    [GeneratedRegex( "^MinimumVisualStudioVersion\\s*=\\s*([0-9\\.]+)", RegexOptions.IgnoreCase, "en-US" )]
    private static partial Regex MinimumVisualStudioVersionRegex();
}
