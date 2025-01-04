namespace SparkDevNetwork.Rock.DevTool.DevEnvironment;

/// <summary>
/// Raised when an operation is attempted on an environment that is not valid.
/// </summary>
class InvalidEnvironmentException : Exception
{
    /// <summary>
    /// Creates a new instance of <see cref="InvalidEnvironmentException"/>
    /// with the specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidEnvironmentException( string message )
        : base( message )
    {
    }
}
