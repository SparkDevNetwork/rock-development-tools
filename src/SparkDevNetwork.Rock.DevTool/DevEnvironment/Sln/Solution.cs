namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// Defines the basic structure of a solution file.
/// </summary>
class Solution
{
    /// <summary>
    /// The header details of this solution.
    /// </summary>
    public Header Header { get; } = new Header();

    /// <summary>
    /// The global details of the solution.
    /// </summary>
    public Global Global { get; } = new Global();

    /// <summary>
    /// The projects contained within the solution.
    /// </summary>
    public List<Project> Projects { get; } = [];

    /// <summary>
    /// Adds a project to the solution. This also configures the build settings. If
    /// an existing project with the same name already exists then it will be
    /// returned instead of a new project being added.
    /// </summary>
    /// <param name="projectName">The name of the project.</param>
    /// <param name="projectFile">The relative path to the project file.</param>
    /// <param name="projectType">The type of project.</param>
    /// <returns>A new or existing instance of <see cref="Project"/>.</returns>
    public Project AddProject( string projectName, string projectFile, Guid projectType )
    {
        var project = Projects.FirstOrDefault( p => p.Name == projectName );

        if ( project != null )
        {
            return project;
        }

        project = new Project
        {
            Name = projectName,
            ProjectType = projectType,
            ProjectFile = projectFile
        };

        Projects.Add( project );

        var solutionConfiguration = Global.GetOrAddSection( "SolutionConfigurationPlatforms", GlobalSection.PreSolution );
        var projectConfiguration = Global.GetOrAddSection( "ProjectConfigurationPlatforms", GlobalSection.PostSolution );

        if ( !solutionConfiguration.Properties.Any( p => p.Name == "Debug|Any CPU" ) )
        {
            solutionConfiguration.Properties.Add( new PropertyValue
            {
                Name = "Debug|Any CPU",
                Value = "Debug|Any CPU"
            } );
        }

        if ( !solutionConfiguration.Properties.Any( p => p.Name == "Release|Any CPU" ) )
        {
            solutionConfiguration.Properties.Add( new PropertyValue
            {
                Name = "Release|Any CPU",
                Value = "Release|Any CPU"
            } );
        }

        projectConfiguration.Properties.Add( new PropertyValue
        {
            Name = $"{project.ProjectGuid.ToSolution()}.Debug|Any CPU.ActiveCfg",
            Value = "Debug|Any CPU"
        } );

        projectConfiguration.Properties.Add( new PropertyValue
        {
            Name = $"{project.ProjectGuid.ToSolution()}.Debug|Any CPU.Build.0",
            Value = "Debug|Any CPU"
        } );

        projectConfiguration.Properties.Add( new PropertyValue
        {
            Name = $"{project.ProjectGuid.ToSolution()}.Release|Any CPU.ActiveCfg",
            Value = "Release|Any CPU"
        } );

        projectConfiguration.Properties.Add( new PropertyValue
        {
            Name = $"{project.ProjectGuid.ToSolution()}.Release|Any CPU.Build.0",
            Value = "Release|Any CPU"
        } );

        return project;
    }
}
