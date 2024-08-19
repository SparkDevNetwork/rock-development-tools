namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// The global block in a solution file.
/// </summary>
class Global : ISolutionItemWriter
{
    /// <summary>
    /// The global sections that have been defined.
    /// </summary>
    public List<GlobalSection> Sections { get; } = [];

    /// <inheritdoc/>
    public void Write( SlnWriter writer )
    {
        writer.WriteLine( "Global" );
        writer.BeginIndentSection();

        foreach ( var section in Sections )
        {
            section.Write( writer );
        }

        writer.EndIndentSection();
        writer.WriteLine( "EndGlobal" );
    }

    /// <summary>
    /// Gets an existing section or adds a new section.
    /// </summary>
    /// <param name="sectionName">The name of the section to get or add.</param>
    /// <param name="location">The location of the section if it needs to be added.</param>
    /// <returns>An instance of <see cref="GlobalSection"/>.</returns>
    public GlobalSection GetOrAddSection( string sectionName, string location )
    {
        var section = Sections.FirstOrDefault( s => s.Name == sectionName );

        if ( section != null )
        {
            return section;
        }

        section = new GlobalSection
        {
            Name = sectionName,
            Location = location
        };

        Sections.Add( section );

        return section;
    }
}
