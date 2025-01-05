namespace SparkDevNetwork.Framework.Builder.Git;

/// <summary>
/// A reference is a commit hash and a ref name.
/// </summary>
class GitReference
{
    /// <summary>
    /// The commit hash of this reference.
    /// </summary>
    public required string CommitHash { get; init; }

    /// <summary>
    /// The ref name of this reference, such as 'refs/tags/v1.0.0'.
    /// </summary>
    public required string Ref { get; init; }
}
