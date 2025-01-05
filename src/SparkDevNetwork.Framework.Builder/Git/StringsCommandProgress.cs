using SparkDevNetwork.Framework.Builder.Executor;

namespace SparkDevNetwork.Framework.Builder.Git;

/// <summary>
/// A progress reporter that collects all the output from the operation as a
/// list of strings representing each line of text.
/// </summary>
class StringsCommandProgress : ICommandProgress
{
    public List<string> Output = [];

    /// <inheritdoc/>
    public void OnStarted()
    {
    }

    /// <inheritdoc/>
    public void OnProgress( string text )
    {
        Output.Add( text );
    }

    /// <inheritdoc/>
    public void OnCompleted()
    {
    }
}
