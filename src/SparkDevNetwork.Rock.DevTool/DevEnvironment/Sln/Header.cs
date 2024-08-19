namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// The header of a solution file. This contains information used by Visual
/// Studio to identify the file.
/// </summary>
class Header : ISolutionItemWriter
{
    /// <summary>
    /// The version of Visual Studio that wrote this file.
    /// </summary>
    public Version VisualStudioVersion { get; set; } = new( 17, 0, 31903, 59 );

    /// <summary>
    /// The minimum version of Visual Studio required to read this file.
    /// </summary>
    public Version MinimumVisualStudioVersion { get; set; } = new( 10, 0, 40219, 1 );

    /// <inheritdoc/>
    public void Write( SlnWriter writer )
    {
        writer.WriteLine( string.Empty );
        writer.WriteLine( "Microsoft Visual Studio Solution File, Format Version 12.00" );
        writer.WriteLine( $"# Visual Studio Version {VisualStudioVersion.Major}" );
        writer.WriteLine( $"VisualStudioVersion = {VisualStudioVersion}" );
        writer.WriteLine( $"MinimumVisualStudioVersion = {MinimumVisualStudioVersion}" );
    }
}
