namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// A named project section that contains property values.
/// </summary>
class ProjectSection : Section
{
    /// <summary>
    /// The location specifier for <c>preProject</c>.
    /// </summary>
    public const string PreProject = "preProject";

    /// <summary>
    /// The location specifier for <c>postProject</c>.
    /// </summary>
    public const string PostProject = "postProject";

    /// <inheritdoc/>
    protected override string Prefix => "Project";

    /// <inheritdoc/>
    protected override List<string> QuotedSections => ["WebsiteProperties"];
}
