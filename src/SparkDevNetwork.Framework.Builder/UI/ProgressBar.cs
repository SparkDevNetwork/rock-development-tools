namespace SparkDevNetwork.Framework.Builder.UI;

/// <summary>
/// A progress bar with a determined end position.
/// </summary>
sealed class ProgressBar
{
    #region Fields

    /// <summary>
    /// The stage of the progress. This allows the caller to essentially
    /// have multiple progress bars in a single bar.
    /// </summary>
    private string? _stage;

    /// <summary>
    /// The name of the task being executed.
    /// </summary>
    private readonly string _taskName;

    /// <summary>
    /// The current progress step towards the total.
    /// </summary>
    private int _step;

    /// <summary>
    /// The total number of steps to complete the task.
    /// </summary>
    private int _totalSteps;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of <see cref="ProgressBar"/>. 
    /// </summary>
    /// <param name="taskName">The name of the task that will be executed.</param>
    /// <param name="totalNumber">The total number of steps in the task.</param>
    public ProgressBar( string taskName, int totalNumber )
    {
        _taskName = taskName;
        _totalSteps = totalNumber;
        _step = 0;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Runs the task and displays a progress bar until it completes.
    /// </summary>
    /// <param name="taskName">The name of the task to display.</param>
    /// <param name="totalSteps">The number of steps in this task.</param>
    /// <param name="executor">The action to execute.</param>
    public static void Run( string taskName, int totalSteps, Func<ProgressBar, bool> executor )
    {
        var bar = new ProgressBar( taskName, totalSteps );

        bar.Start();

        var result = executor( bar );

        bar.Stop( result );
    }

    /// <summary>
    /// Runs the task and displays a progress bar until it completes.
    /// </summary>
    /// <param name="taskName">The name of the task to display.</param>
    /// <param name="totalSteps">The number of steps in this task.</param>
    /// <param name="executor">The action to execute.</param>
    public static async Task Run( string taskName, int totalSteps, Func<ProgressBar, Task<bool>> executor )
    {
        var bar = new ProgressBar( taskName, totalSteps );

        bar.Start();

        var result = await executor( bar );

        bar.Stop( result );
    }

    /// <summary>
    /// Sets the current step achieved in this progress bar.
    /// </summary>
    /// <param name="step">The step number.</param>
    public void SetStep( int step )
    {
        if ( _step == step )
        {
            return;
        }

        var lastPercent = Math.Floor( _step / ( float ) _totalSteps * 100 );
        _step = step;

        if ( Math.Floor( _step / ( float ) _totalSteps * 100 ) != lastPercent )
        {
            UpdateBar();
        }
    }

    /// <summary>
    /// Moves to the next step.
    /// </summary>
    public void NextStep()
    {
        SetStep( _step + 1 );
    }

    /// <summary>
    /// Sets the total number of steps and optionally a new stage. This will
    /// also reset the current step to 0.
    /// </summary>
    /// <param name="totalSteps">The new total number of steps.</param>
    /// <param name="stage">The stage to display with the task name.</param>
    public void SetTotal( int totalSteps, string? stage )
    {
        _totalSteps = totalSteps;
        _stage = stage;

        UpdateBar();
    }

    /// <summary>
    /// Start the progress bar displaying.
    /// </summary>
    private void Start()
    {
        UpdateBar();
    }

    /// <summary>
    /// Stop the progress bar and output the final state on screen.
    /// </summary>
    /// <param name="success"><c>true</c> if the task was successful.</param>
    private void Stop( bool success )
    {
        Console.Out.Write( "\u001b[2K\r" );

        if ( success )
        {
            Console.Out.WriteLine( $"{_taskName} \u001b[38;5;10m\u2714\u001b[0m" );
        }
        else
        {
            Console.Out.WriteLine( $"{_taskName} \u001b[38;5;9m\u2715\u001b[0m" );
        }
    }

    /// <summary>
    /// Update the progress bar on screen.
    /// </summary>
    private void UpdateBar()
    {
        var msg = string.IsNullOrWhiteSpace( _stage )
            ? _taskName
            : $"{_taskName} [{_stage}]";
        var percent = Math.Max( 0, Math.Min( 100, Math.Floor( _step / ( float ) _totalSteps * 100 ) ) );
        var bar = new string( '\u2588', ( int ) Math.Floor( percent * 40 / 100.0f ) );
        var fill = new string( ' ', 40 - bar.Length );

        Console.Out.Write( $"\u001b[2K\r{msg} [\u001b[38;5;208m{bar}\u001b[0m{fill}] {percent}%" );
    }

    #endregion
}
