using System.Text.RegularExpressions;

namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// A parser for a named <see cref="ProjectSection"/> in the solution.
/// </summary>
partial class ProjectSectionParser : ISolutionItemParser
{
    /// <inheritdoc/>
    public bool Parse( string line, SlnReader reader )
    {
        if ( reader.CurrentContext is not Project project )
        {
            return false;
        }

        var match = ProjectSectionRegex().Match( line );

        if ( !match.Success )
        {
            return false;
        }

        var section = new ProjectSection
        {
            Name = match.Groups[1].Value,
            Location = match.Groups[2].Value,
        };

        project.Sections.Add( section );

        reader.ParseBlock( "EndProjectSection", section );

        return true;
    }

    [GeneratedRegex( "^ProjectSection\\(([^\\)]+)\\)\\s*=\\s*(.+)", RegexOptions.IgnoreCase, "en-US" )]
    private static partial Regex ProjectSectionRegex();
}
