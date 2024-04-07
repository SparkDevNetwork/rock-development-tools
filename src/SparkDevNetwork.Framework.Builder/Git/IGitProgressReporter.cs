namespace SparkDevNetwork.Framework.Builder.Git;

/// <summary>
/// Provides progress feedback on Git operations.
/// </summary>
interface IGitProgressReporter
{
    /// <summary>
    /// Called when the operation has started.
    /// </summary>
    void OnStarted();

    /// <summary>
    /// Called when a new message about the git operation is available.
    /// </summary>
    /// <param name="stage">The stage of the operation.</param>
    /// <param name="message">The message that describes the progress.</param>
    void OnProgress( string stage, string message );

    /// <summary>
    /// Called when the operation has completed.
    /// </summary>
    void OnCompleted();
}
