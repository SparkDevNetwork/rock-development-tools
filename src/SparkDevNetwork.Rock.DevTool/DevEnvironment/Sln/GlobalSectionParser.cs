using System.Text.RegularExpressions;

namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// The parser for a named <see cref="GlobalSection"/> in a solution file.
/// </summary>
partial class GlobalSectionParser : ISolutionItemParser
{
    /// <inheritdoc/>
    public bool Parse( string line, SlnReader reader )
    {
        if ( reader.CurrentContext is not Global glob )
        {
            return false;
        }

        var match = GlobalSectionRegex().Match( line );

        if ( !match.Success )
        {
            return false;
        }

        var section = new GlobalSection
        {
            Name = match.Groups[1].Value,
            Location = match.Groups[2].Value,
        };

        glob.Sections.Add( section );

        reader.ParseBlock( "EndGlobalSection", section );

        return true;
    }

    [GeneratedRegex( "^GlobalSection\\(([^\\)]+)\\)\\s*=\\s*(.+)", RegexOptions.IgnoreCase, "en-US" )]
    private static partial Regex GlobalSectionRegex();
}
