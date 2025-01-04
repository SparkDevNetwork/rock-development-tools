namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// A parser for a named GlobalSection in a solution file.
/// </summary>
class GlobalSection : Section
{
    /// <summary>
    /// The location specifier for <c>preSolution</c>.
    /// </summary>
    public const string PreSolution = "preSolution";

    /// <summary>
    /// The location specifier for <c>postSolution</c>.
    /// </summary>
    public const string PostSolution = "postSolution";

    /// <inheritdoc/>
    protected override string Prefix => "Global";
}
