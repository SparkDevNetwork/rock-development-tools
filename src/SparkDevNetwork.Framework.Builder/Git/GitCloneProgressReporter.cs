using System.Text.RegularExpressions;

using SparkDevNetwork.Framework.Builder.Executor;
using SparkDevNetwork.Framework.Builder.UI;

namespace SparkDevNetwork.Framework.Builder.Git;

/// <summary>
/// Git progress reporter that will handle clone operations.
/// </summary>
partial class GitCloneProgressReporter : ICommandProgress
{
    #region Fields

    /// <summary>
    /// The last stage message that was received.
    /// </summary>
    private string _lastStage = string.Empty;

    private readonly ProgressBar _progressBar;

    [GeneratedRegex( @"([\w\d ]+):\s*(?:\d+%\s)?\((\d+)\/(\d+)\)" )]
    private static partial Regex GitProgressRegExp();

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of <see cref="GitCloneProgressReporter"/>. 
    /// </summary>
    /// <param name="progressBar">The progress bar that will be used to display progress.</param>
    public GitCloneProgressReporter( ProgressBar progressBar )
    {
        _progressBar = progressBar;
    }

    #endregion

    #region ICommandProgress

    /// <inheritdoc/>
    public void OnStarted()
    {
    }

    /// <inheritdoc/>
    public void OnProgress( string message )
    {
        if ( message.StartsWith( "remote: " ) )
        {
            message = message[8..];
        }

        var match = GitProgressRegExp().Match( message );

        if ( !match.Success )
        {
            return;
        }

        var stage = match.Groups[1].Value;

        if ( !int.TryParse( match.Groups[2].Value, out var step ) )
        {
            return;
        }

        if ( !int.TryParse( match.Groups[3].Value, out var totalSteps ) )
        {
            return;
        }

        if ( _lastStage != stage )
        {
            _lastStage = stage;

            _progressBar.SetStep( step, totalSteps, stage );
        }
        else
        {
            _progressBar.SetStep( step, totalSteps );
        }
    }

    /// <inheritdoc/>
    public void OnCompleted()
    {
    }

    #endregion
}
