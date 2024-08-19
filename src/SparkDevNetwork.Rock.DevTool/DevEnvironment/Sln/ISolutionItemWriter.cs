namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// Defines an object that supports writing itself to a solution file.
/// </summary>
interface ISolutionItemWriter
{
    /// <summary>
    /// Writes this item to the writer.
    /// </summary>
    /// <param name="writer">The solution writer.</param>
    void Write( SlnWriter writer );
}
