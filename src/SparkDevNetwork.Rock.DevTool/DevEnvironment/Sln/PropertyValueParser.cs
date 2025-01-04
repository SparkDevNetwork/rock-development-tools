using System.Text.RegularExpressions;

namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// A parser for a <see cref="PropertyValue"/> in the solution.
/// </summary>
partial class PropertyValueParser : ISolutionItemParser
{
    /// <inheritdoc/>
    public bool Parse( string line, SlnReader reader )
    {
        if ( reader.CurrentContext is not Section section )
        {
            return false;
        }

        // Property names can have a space in them, so include trailing spaces
        // for the match and then we will trim it later.
        var match = PropertyValueRegex().Match( line );

        if ( !match.Success )
        {
            return false;
        }

        var property = new PropertyValue
        {
            Name = match.Groups[1].Value.Trim(),
            Value = match.Groups[2].Value
        };

        if ( section.IsQuotedSection() )
        {
            property.Value = property.Value.Trim( '"' );
        }

        section.Properties.Add( property );

        return true;
    }

    [GeneratedRegex( "([^=]+)=\\s*(.+)", RegexOptions.IgnoreCase, "en-US" )]
    private static partial Regex PropertyValueRegex();
}
