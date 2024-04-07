namespace SparkDevNetwork.Framework.Builder.Git;

/// <summary>
/// Git progress reporter that will print progress to the console.
/// </summary>
class ConsoleGitProgressReporter : IGitProgressReporter
{
    #region Fields

    /// <summary>
    /// The last stage message that was received.
    /// </summary>
    private string _lastStage = string.Empty;

    /// <summary>
    /// The last message that was skipped because it happened too quickly. This
    /// is used to print the message when the stage ends so we don't have a
    /// partial message like 438/458 rather than 458/458.
    /// </summary>
    private string _lastSkippedMessage = string.Empty;

    /// <summary>
    /// The last date we printed a message. This allows us to skip messages that
    /// come too fast.
    /// </summary>
    private DateTime _lastPrinted = DateTime.MinValue;

    /// <summary>
    /// Our best guess at if this is a terminal or not.
    /// </summary>
    private readonly bool _isTerminal = !Console.IsOutputRedirected;

    #endregion

    #region Methods

    /// <summary>
    /// Prints the message with appropriate formatting based on terminal
    /// settings.
    /// </summary>
    /// <param name="stage">The stage of the operation.</param>
    /// <param name="message">The message that describes the progress.</param>
    private void PrintMessage( string stage, string message )
    {
        if ( _isTerminal )
        {
            Console.Write( $"\r{stage}: {message}" );
        }
        else
        {
            Console.WriteLine( $"{stage}: {message}" );
        }
    }

    /// <summary>
    /// Print the last skipped message if there was one.
    /// </summary>
    /// <returns><c>true</c> if a message was printed; otherwise <c>false</c>.</returns>
    private bool PrintSkippedMessage()
    {
        if ( _lastSkippedMessage != string.Empty )
        {
            PrintMessage( _lastStage, _lastSkippedMessage );

            _lastSkippedMessage = string.Empty;

            return true;
        }

        return false;
    }

    #endregion

    #region IGitProgressReporter

    /// <inheritdoc/>
    public void OnStarted()
    {
    }

    /// <inheritdoc/>
    public void OnProgress( string stage, string message )
    {
        if ( _lastStage != stage )
        {
            PrintSkippedMessage();

            if ( _lastPrinted != DateTime.MinValue && _isTerminal )
            {
                Console.WriteLine();
            }

            _lastStage = stage;
        }
        else if ( _lastPrinted.Add( TimeSpan.FromMilliseconds( 100 ) ) > DateTime.Now )
        {
            _lastSkippedMessage = message;
            return;
        }

        PrintMessage( stage, message );

        _lastPrinted = DateTime.Now;
        _lastSkippedMessage = string.Empty;
    }

    /// <inheritdoc/>
    public void OnCompleted()
    {
        if ( PrintSkippedMessage() || _lastPrinted != DateTime.MinValue )
        {
            if ( _isTerminal )
            {
                Console.WriteLine();
            }
        }
    }

    #endregion
}
