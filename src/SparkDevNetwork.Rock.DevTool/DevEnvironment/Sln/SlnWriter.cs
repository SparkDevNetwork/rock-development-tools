namespace SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

/// <summary>
/// Writes a <see cref="Solution"/> to a string or file.
/// </summary>
class SlnWriter
{
    /// <summary>
    /// The current writer.
    /// </summary>
    private TextWriter _writer = new StringWriter();

    /// <summary>
    /// The current indentation level.
    /// </summary>
    private int _indentLevel = 0;

    /// <summary>
    /// Converts the <see cref="Solution"/> object into a string that represents
    /// the content of an sln file.
    /// </summary>
    /// <param name="solution">The solution to write.</param>
    /// <returns>A new string that contains the contents of the solution file.</returns>
    public string WriteToString( Solution solution )
    {
        _writer = new StringWriter();

        solution.Header.Write( this );

        foreach ( var item in solution.Projects )
        {
            item.Write( this );
        }

        solution.Global.Write( this );

        _writer.Flush();

        return _writer.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Increases the indent level of the written content by one level.
    /// </summary>
    public void BeginIndentSection()
    {
        _indentLevel++;
    }

    /// <summary>
    /// Decreases the indent level of the written content by one level.
    /// </summary>
    public void EndIndentSection()
    {
        _indentLevel--;
    }

    /// <summary>
    /// Writes a single line of text to the output. This takes care of any
    /// indentation required.
    /// </summary>
    /// <param name="text">The text to write.</param>
    public void WriteLine( string text )
    {
        if ( _indentLevel > 0 )
        {
            _writer.Write( new string( '\t', _indentLevel ) );
        }

        _writer.Write( text );
        _writer.Write( "\r\n" );
    }
}
