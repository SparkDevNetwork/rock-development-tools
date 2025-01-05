using System.Text.RegularExpressions;

using Semver;

using SparkDevNetwork.Framework.Builder.Git;

namespace SparkDevNetwork.Framework.Builder;

/// <summary>
/// Provides methods used when building the framework packages.
/// </summary>
partial class RockBuilder
{
    /// <summary>
    /// The regular expression that will be used to match version tags
    /// in git refs.
    /// </summary>
    [GeneratedRegex( @"refs\/tags\/((\d+)\.(\d+)\.(\d+)([\.-].*)?)" )]
    private static partial Regex VersionRegExp();

    /// <summary>
    /// Gets the possible version tags that can be used when building Rock.
    /// </summary>
    /// <returns>A list of <see cref="RockVersionTag"/> objects.</returns>
    public static List<RockVersionTag> GetRockVersions()
    {
        var minimumVersion = new SemVersion( 1, 16, 0 );

        return GitCommand
            .ListRemoteReferences( "https://github.com/SparkDevNetwork/Rock", "refs/tags/[0-9]*.*" )
            .Select( r => new
            {
                Match = VersionRegExp().Match( r.Ref ),
                r.CommitHash
            } )
            .Where( r => r.Match.Success )
            .Select( r => new
            {
                r.CommitHash,
                Tag = r.Match.Groups[1].Value,
                Major = int.Parse( r.Match.Groups[2].Value ),
                Minor = int.Parse( r.Match.Groups[3].Value ),
                Patch = int.Parse( r.Match.Groups[4].Value ),
                Prerelease = ParsePrereleaseSuffix( r.Match.Groups[5].Value )
            } )
            .Select( v => new RockVersionTag
            {
                CommitHash = v.CommitHash,
                Version = new SemVersion( v.Major, v.Minor, v.Patch, v.Prerelease ),
                Tag = v.Tag
            } )
            .Where( v => v.Version.ComparePrecedenceTo( minimumVersion ) >= 0 )
            .ToList();
    }

    public static void DownloadRock( RockVersionTag version )
    {

    }

    /// <summary>
    /// Parse a suffix on the version tag into a set of pre-release comonents.
    /// </summary>
    /// <param name="suffix">The suffix on the version tag.</param>
    /// <returns>A list of strings.</returns>
    private static List<string> ParsePrereleaseSuffix( string suffix )
    {
        var components = new List<string>();
        var component = string.Empty;

        if ( string.IsNullOrEmpty( suffix ) )
        {
            return components;
        }

        components.Add( "rc" );

        for ( int i = 0; i < suffix.Length; i++ )
        {
            var ch = suffix[i];

            if ( ch == '.' || ch == '-' )
            {
                if ( component != string.Empty )
                {
                    components.Add( component );
                }

                component = string.Empty;
            }
            else if ( component == string.Empty )
            {
                component += ch;
            }
            else
            {
                if ( char.IsDigit( component[0] ) == char.IsDigit( ch ) )
                {
                    component += ch;
                }
                else
                {
                    components.Add( component );
                    component = string.Empty;
                    component += ch;
                }
            }
        }

        if ( component != string.Empty )
        {
            components.Add( component );
        }


        return components;
    }
}
