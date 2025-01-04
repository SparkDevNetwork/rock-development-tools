namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// A parser for a solution item.
/// </summary>
interface ISolutionItemParser
{
    /// <summary>
    /// Attempts to parse the line for the reader.
    /// </summary>
    /// <param name="line">The line to be parsed.</param>
    /// <param name="reader">The reader.</param>
    /// <returns><c>true</c> if the line was parsed; otherwise <c>false</c>.</returns>
    bool Parse( string line, SlnReader reader );
}
