namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// A generic use section that can exist in various places in the solution.
/// </summary>
abstract class Section : ISolutionItemWriter
{
    /// <summary>
    /// The name of the section.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The location specifier for this section.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// The properties and values in this section.
    /// </summary>
    public List<PropertyValue> Properties { get; } = [];

    /// <summary>
    /// The prefix to use for the start and end tags.
    /// </summary>
    protected abstract string Prefix { get; }

    /// <summary>
    /// Section names whose values should be wrapped in double quotes.
    /// </summary>
    protected virtual List<string> QuotedSections => [];

    /// <inheritdoc/>
    public void Write( SlnWriter writer )
    {
        writer.WriteLine( $"{Prefix}Section({Name}) = {Location}" );
        writer.BeginIndentSection();

        foreach ( var item in Properties )
        {
            if ( IsQuotedSection() )
            {
                writer.WriteLine( $"{item.Name} = \"{item.Value}\"" );
            }
            else
            {
                writer.WriteLine( $"{item.Name} = {item.Value}" );
            }
        }

        writer.EndIndentSection();
        writer.WriteLine( $"End{Prefix}Section" );
    }

    /// <summary>
    /// Determines if this section is one that will wrap it's values in double
    /// quotes.
    /// </summary>
    /// <returns><c>true</c> if this section is quoted; otherwise <c>false</c>.</returns>
    public bool IsQuotedSection()
    {
        return QuotedSections.Contains( Name, StringComparer.OrdinalIgnoreCase );
    }

    /// <summary>
    /// Adds a new or updates an existing property.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="value">The value of the property.</param>
    public void SetProperty( string name, string value )
    {
        var property = Properties.FirstOrDefault( p => p.Name == name );

        if ( property == null )
        {
            property = new PropertyValue
            {
                Name = name
            };

            Properties.Add( property );
        }

        property.Value = value;
    }
}
