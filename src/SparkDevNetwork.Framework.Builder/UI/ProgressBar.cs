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
    /// Determines if the task completed successfully.
    /// </summary>
    private bool _taskStatus = true;

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
    public static void Run( string taskName, int totalSteps, Action<ProgressBar> executor )
    {
        var bar = new ProgressBar( taskName, totalSteps );

        bar.Start();

        try
        {
            executor( bar );

            bar.Stop( bar._taskStatus );
        }
        catch
        {
            bar.Stop( false );
            throw;
        }
    }

    /// <summary>
    /// Runs the task and displays a progress bar until it completes.
    /// </summary>
    /// <param name="taskName">The name of the task to display.</param>
    /// <param name="totalSteps">The number of steps in this task.</param>
    /// <param name="executor">The action to execute.</param>
    public static async Task Run( string taskName, int totalSteps, Func<ProgressBar, Task> executor )
    {
        var bar = new ProgressBar( taskName, totalSteps );

        bar.Start();

        try
        {
            await executor( bar );

            bar.Stop( bar._taskStatus );
        }
        catch
        {
            bar.Stop( false );
            throw;
        }
    }

    /// <summary>
    /// Indicates that the task has failed and should report an error.
    /// </summary>
    public void Fail()
    {
        _taskStatus = false;
    }

    /// <summary>
    /// Sets the current step achieved in this progress bar.
    /// </summary>
    /// <param name="step">The step number.</param>
    /// <param name="totalSteps">If not <c>null</c> then the total number of steps will be updated.</param>
    /// <param name="stage">If not <c>null</c> then the stage will be updated.
    public void SetStep( int step, int? totalSteps = null, string? stage = null )
    {
        if ( _step == step )
        {
            return;
        }

        var stageChanged = false;
        var lastPercent = Math.Floor( _step / ( float ) _totalSteps * 100 );

        _step = step;
        _totalSteps = totalSteps ?? _totalSteps;

        if ( !string.IsNullOrEmpty( stage ) && stage != _stage )
        {
            _stage = stage;
            stageChanged = true;
        }

        var currentPercent = Math.Floor( _step / ( float ) _totalSteps * 100 );

        if ( currentPercent != lastPercent || stageChanged )
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
