using Semver;

namespace SparkDevNetwork.Framework.Builder;

/// <summary>
/// A single rock version tag in git.
/// </summary>
class RockVersionTag
{
    /// <summary>
    /// The Rock version this tag represents.
    /// </summary>
    public required SemVersion Version { get; init; }

    /// <summary>
    /// The tag name in git.
    /// </summary>
    public required string Tag { get; init; }

    /// <summary>
    /// The commit hash.
    /// </summary>
    public required string CommitHash { get; init; }
}
