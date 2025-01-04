namespace SparkDevNetwork.Rock.DevTool;

/// <summary>
/// Exception that is thrown when no credentials are available.
/// </summary>
class NoCredentialsException : Exception
{
    /// <summary>
    /// Creates a new exception to indicate that no credentials are available.
    /// </summary>
    public NoCredentialsException()
        : base( "Authorization was required but not available." )
    {
    }
}
