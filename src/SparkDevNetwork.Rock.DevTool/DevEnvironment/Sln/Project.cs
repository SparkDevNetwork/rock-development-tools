namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// Defines a project in the solution.
/// </summary>
class Project : ISolutionItemWriter
{
    /// <summary>
    /// The project type for C# projects.
    /// </summary>
    public static readonly Guid ProjectTypeCSharp = new( "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC" );

    /// <summary>
    /// The project type for new JavaScript projects using the esproj format.
    /// </summary>
    public static readonly Guid ProjectTypeJavaScript = new( "54A90642-561A-4BB1-A94E-469ADEE60C69" );

    /// <summary>
    /// The project type for a Web Site.
    /// </summary>
    public static readonly Guid ProjectTypeWebSite = new( "E24C65DC-7377-472B-9ABA-BC803B73C61A" );

    /// <summary>
    /// The type of project.
    /// </summary>
    public Guid ProjectType { get; set; }

    /// <summary>
    /// The name of the project in the solution.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The file (or path for a website) relative to the solution folder.
    /// </summary>
    public string ProjectFile { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier for the project.
    /// </summary>
    public Guid ProjectGuid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The configuration sections for the project.
    /// </summary>
    public List<ProjectSection> Sections { get; } = [];

    /// <inheritdoc/>
    public void Write( SlnWriter writer )
    {
        writer.WriteLine( $"Project(\"{ProjectType.ToSolution()}\") = \"{Name}\", \"{ProjectFile}\", \"{ProjectGuid.ToSolution()}\"" );
        writer.BeginIndentSection();

        foreach ( var section in Sections )
        {
            section.Write( writer );
        }

        writer.EndIndentSection();
        writer.WriteLine( "EndProject" );
    }

    /// <summary>
    /// Gets an existing section or adds a new section.
    /// </summary>
    /// <param name="sectionName">The name of the section to get or add.</param>
    /// <param name="location">The location of the section if it needs to be added.</param>
    /// <returns>An instance of <see cref="ProjectSection"/>.</returns>
    public ProjectSection GetOrAddSection( string sectionName, string location )
    {
        var section = Sections.FirstOrDefault( s => s.Name == sectionName );

        if ( section != null )
        {
            return section;
        }

        section = new ProjectSection
        {
            Name = sectionName,
            Location = location
        };

        Sections.Add( section );

        return section;
    }

    /// <summary>
    /// Adds a project dependency to this project. This is a solution level
    /// dependency which will cause the specified project to be built
    /// before this project.
    /// </summary>
    /// <param name="projectGuid">The project this project depends on.</param>
    public void AddProjectDependency( Guid projectGuid )
    {
        var projectDependencies = GetOrAddSection( "Project Dependencies", ProjectSection.PostProject );

        if ( !projectDependencies.Properties.Any( d => d.Name == projectGuid.ToSolution() ) )
        {
            projectDependencies.Properties.Add( new PropertyValue
            {
                Name = projectGuid.ToSolution(),
                Value = projectGuid.ToSolution()
            } );
        }
    }

    /// <summary>
    /// Removes a project dependency from this project.
    /// </summary>
    /// <param name="projectGuid">The project this project no longer depends on.</param>
    public void RemoveProjectDependency( Guid projectGuid )
    {
        var projectDependencies = GetOrAddSection( "Project Dependencies", ProjectSection.PostProject );

        projectDependencies.Properties.RemoveAll( d => d.Name == projectGuid.ToSolution() );
    }
}
