namespace SparkDevNetwork.Framework.Builder.UI;

/// <summary>
/// A progress bar with an indeterminate state.
/// </summary>
sealed class IndeterminateBar
{
    #region Fields

    /// <summary>
    /// The starting position of the filled bar within the total bar space.
    /// </summary>
    private int _tickPosition;

    /// <summary>
    /// The name of the task that will be displayed before the progress bar.
    /// </summary>
    private readonly string _taskName;

    /// <summary>
    /// The background task that is updating the progress bar.
    /// </summary>
    private Task? _timerTask;

    /// <summary>
    /// Used to cancel a running progress bar.
    /// </summary>
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// The string that represents the filled bar.
    /// </summary>
    private const string BarString = "\u2592\u2593\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2593\u2592";

    /// <summary>
    /// The width of the inner progress bar.
    /// </summary>
    private const int TotalBarWidth = 40;

    /// <summary>
    /// The duration in milliseconds between bar updates.
    /// </summary>
    private const int TickDuration = 50;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of <see cref="IndeterminateBar"/>. 
    /// </summary>
    /// <param name="taskName">The name of the task that is running.</param>
    private IndeterminateBar( string taskName )
    {
        _taskName = taskName;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Runs the task and displays a progress bar until it completes.
    /// </summary>
    /// <param name="taskName">The name of the task to display.</param>
    /// <param name="executor">The action to execute.</param>
    public static void Run( string taskName, Func<IndeterminateBar, bool> executor )
    {
        var bar = new IndeterminateBar( taskName );

        bar.Start();

        try
        {
            var result = executor( bar );

            bar.Stop( result );
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
    /// <param name="executor">The action to execute.</param>
    public static async Task Run( string taskName, Func<IndeterminateBar, Task<bool>> executor )
    {
        var bar = new IndeterminateBar( taskName );

        bar.Start();

        try
        {
            var result = await executor( bar );

            bar.Stop( result );
        }
        catch
        {
            bar.Stop( false );
            throw;
        }
    }

    /// <summary>
    /// Starts the progress bar displaying on screen.
    /// </summary>
    private void Start()
    {
        if ( _timerTask != null )
        {
            throw new InvalidOperationException( "Progress bar is already running a task." );
        }

        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        _timerTask = Task.Run( async () => await TickTimer( cancellationToken ), cancellationToken );
    }

    /// <summary>
    /// Stops the progress bar by updating it with the final success state.
    /// </summary>
    /// <param name="success"><c>true</c> if the task completed successfully.</param>
    private void Stop( bool success )
    {
        _cancellationTokenSource?.Cancel();

        var padLength = 1 + TotalBarWidth + 1;
        var pad = new string( ' ', padLength );

        if ( success )
        {
            Console.WriteLine( $"\r{_taskName} \u001b[38;5;10m\u2714\u001b[0m{pad}" );
        }
        else
        {
            Console.WriteLine( $"\r{_taskName} \u001b[38;5;9m\u2715\u001b[0m{pad}" );
        }

        _timerTask?.Wait();
    }

    /// <summary>
    /// Handles the timing of when the progress bar is updated.
    /// </summary>
    /// <param name="cancellationToken">The token that indicates when the task has finished.</param>
    private async Task TickTimer( CancellationToken cancellationToken )
    {
        try
        {
            while ( !cancellationToken.IsCancellationRequested )
            {
                UpdateBar();

                await Task.Delay( TickDuration, cancellationToken );
            }
        }
        catch ( TaskCanceledException )
        {
        }
        catch ( Exception ex )
        {
            Console.WriteLine( ex );
        }
    }

    /// <summary>
    /// Updates the progress bar on screen.
    /// </summary>
    private void UpdateBar()
    {
        // Bar size needs to account for being off screen to left and right.
        var barSize = BarString.Length;

        var preBar = new string( ' ', Math.Max( 0, Math.Min( TotalBarWidth, _tickPosition ) ) );
        var barStrStart = Math.Min( BarString.Length - 1, Math.Abs( Math.Min( 0, _tickPosition ) ) );

        if ( _tickPosition < 0 )
        {
            barSize = BarString.Length - barStrStart;
        }
        else if ( _tickPosition > ( TotalBarWidth - BarString.Length ) )
        {
            barSize = Math.Max( 0, TotalBarWidth - _tickPosition );
        }

        var barStr = BarString.Substring( barStrStart, barSize );
        var postBar = new string( ' ', TotalBarWidth - barSize - preBar.Length );

        Console.Out.Write( $"\r{_taskName} [{preBar}\u001b[38;5;208m{barStr}\u001b[0m{postBar}]" );

        _tickPosition++;

        if ( _tickPosition >= TotalBarWidth + BarString.Length )
        {
            _tickPosition = -BarString.Length;
        }
    }

    #endregion
}
