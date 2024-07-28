namespace SparkDevNetwork.Rock.DevTool.DevEnvironment;

/// <summary>
/// Raised when an operation is attempted on an plugin that is not valid.
/// </summary>
class InvalidPluginException : Exception
{
    /// <summary>
    /// Creates a new instance of <see cref="InvalidPluginException"/>
    /// with the specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidPluginException( string message )
        : base( message )
    {
    }
}
