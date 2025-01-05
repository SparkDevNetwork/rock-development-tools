namespace SparkDevNetwork.Framework.Builder.Executor;

/// <summary>
/// Provides progress feedback on executed commands.
/// </summary>
interface ICommandProgress
{
    /// <summary>
    /// Called when the command has started.
    /// </summary>
    void OnStarted();

    /// <summary>
    /// Called when a new line of text from the command is available.
    /// </summary>
    /// <param name="text">The text from the command.</param>
    void OnProgress( string text );

    /// <summary>
    /// Called when the command has completed.
    /// </summary>
    void OnCompleted();
}
