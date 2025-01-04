using System.Text.RegularExpressions;

namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// A parser for a <see cref="Project"/> in the solution.
/// </summary>
partial class ProjectParser : ISolutionItemParser
{
    /// <inheritdoc/>
    public bool Parse( string line, SlnReader reader )
    {
        if ( reader.CurrentContext != null )
        {
            return false;
        }

        var match = ProjectRegex().Match( line );

        if ( !match.Success )
        {
            return false;
        }

        var project = new Project
        {
            ProjectType = Guid.Parse( match.Groups[1].Value ),
            Name = match.Groups[2].Value,
            ProjectFile = match.Groups[3].Value,
            ProjectGuid = Guid.Parse( match.Groups[4].Value )
        };

        reader.Solution.Projects.Add( project );

        reader.ParseBlock( "EndProject", project );

        return true;
    }

    [GeneratedRegex( "^Project\\(\"([^\"]+)\"\\)\\s*=\\s*\"([^\"]+)\"\\s*,\\s*\"([^\"]+)\"\\s*,\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase, "en-US" )]
    private static partial Regex ProjectRegex();
}
