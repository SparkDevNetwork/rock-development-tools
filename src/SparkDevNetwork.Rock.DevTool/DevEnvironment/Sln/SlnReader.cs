namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// Handles reading and parsing a solution file into a <see cref="Solution"/>
/// instance.
/// </summary>
class SlnReader
{
    /// <summary>
    /// The current reader for this instance.
    /// </summary>
    private TextReader? _reader;

    /// <summary>
    /// The contexts, that is parent objects, that are being tracked.
    /// </summary>
    private readonly List<object> _contexts = [];

    /// <summary>
    /// The parsers that exist for this instance.
    /// </summary>
    private readonly List<ISolutionItemParser> _parsers =
    [
        new SolutionHeaderParser(),
        new GlobalParser(),
        new GlobalSectionParser(),
        new ProjectParser(),
        new ProjectSectionParser(),
        new PropertyValueParser(),
    ];

    /// <summary>
    /// The current node object handling the indentation level.
    /// </summary>
    public object? CurrentContext => _contexts.LastOrDefault();

    /// <summary>
    /// The solution instance being filled in by the reader. This is only
    /// valid while parsing.
    /// </summary>
    public Solution Solution { get; private set; } = new();

    /// <summary>
    /// Parses the content of a solution file.
    /// </summary>
    /// <param name="content">The content of the solution file to be parsed.</param>
    /// <returns>An instance of <see cref="Solution"/> that represents the parsed solution.</returns>
    public Solution ParseString( string content )
    {
        _reader = new StringReader( content );
        Solution = new Solution();

        try
        {
            string? line;

            while ( ( line = _reader.ReadLine() ) != null )
            {
                ParseLine( line );
            }
        }
        finally
        {
            _reader?.Dispose();
            _reader = null;
        }

        return Solution;
    }

    /// <summary>
    /// Parses a single line of the solution file.
    /// </summary>
    /// <param name="line">The line of text to be parsed.</param>
    private void ParseLine( string line )
    {
        line = line.Trim();

        if ( string.IsNullOrWhiteSpace( line ) )
        {
            return;
        }

        foreach ( var parser in _parsers )
        {
            if ( parser.Parse( line, this ) )
            {
                return;
            }
        }

        throw new Exception( $"Unable to parse existing solution, unexpected line '{line}'." );
    }

    /// <summary>
    /// Parses a block of indented content until the <paramref name="endLine"/> marker is seen.
    /// </summary>
    /// <param name="endLine">The line content that will end the section.</param>
    /// <param name="context">The object that will be pushed onto the context stack.</param>
    public void ParseBlock( string endLine, object context )
    {
        if ( _reader == null )
        {
            throw new InvalidOperationException( "Reader has not been initialized." );
        }

        _contexts.Add( context );

        string? line;

        while ( ( line = _reader.ReadLine() ) != null )
        {
            if ( endLine.Equals( line.Trim(), StringComparison.OrdinalIgnoreCase ) )
            {
                break;
            }

            ParseLine( line );
        }

        _contexts.RemoveAt( _contexts.Count - 1 );
    }
}
