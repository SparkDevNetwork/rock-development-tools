namespace SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation;

using SparkDevNetwork.Rock.CodeGenerator.Documentation;

public class XmlCommentTests
{
    #region Constructor

    [Fact]
    public void Constructor_MarkupContent_SetsCorrectProperties()
    {
        var xml = "<summary>Te<b>s</b>t</summary>";
        var nav = GetNavigator( xml );

        var result = new XmlComment( nav );

        Assert.Equal( "Te<b>s</b>t", result.Content );
        Assert.Equal( "Test", result.PlainText );
    }

    #endregion

    #region GetIndentSize

    [Fact]
    public void GetIndentSize_EmptyText_ReturnsZero()
    {
        var xml = "<summary></summary>";
        var nav = GetNavigator( xml );

        var result = XmlComment.GetIndentSize( nav );

        Assert.Equal( 0, result );
    }

    [Fact]
    public void GetIndentSize_SelfClosingTag_ReturnsZero()
    {
        var xml = "<summary />";
        var nav = GetNavigator( xml );

        var result = XmlComment.GetIndentSize( nav );

        Assert.Equal( 0, result );
    }

    [Fact]
    public void GetIndentSize_SingleLine_ReturnsZero()
    {
        var xml = "<summary>Test</summary>";
        var nav = GetNavigator( xml );

        var result = XmlComment.GetIndentSize( nav );

        Assert.Equal( 0, result );
    }

    [Fact]
    public void GetIndentSize_UnindentedMultilineLine_ReturnsZero()
    {
        var xml = "<summary>\r\nTest\r\n</summary>";
        var nav = GetNavigator( xml );

        var result = XmlComment.GetIndentSize( nav );

        Assert.Equal( 0, result );
    }

    [Fact]
    public void GetIndentSize_IndentedMultilineLine_ReturnsFour()
    {
        var xml = "<summary>\r\n    Test\r\n    </summary>";
        var nav = GetNavigator( xml );

        var result = XmlComment.GetIndentSize( nav );

        Assert.Equal( 4, result );
    }

    #endregion

    #region GetContent

    [Fact]
    public void GetContent_EmptyText_ReturnsEmptyString()
    {
        var xml = "<summary></summary>";
        var nav = GetNavigator( xml );

        var result = XmlComment.GetContent( nav );

        Assert.Equal( string.Empty, result );
    }

    [Fact]
    public void GetContent_SingleLineNode_ReturnsTrimmedText()
    {
        var xml = "<summary> Test </summary>";
        var nav = GetNavigator( xml );

        var result = XmlComment.GetContent( nav );

        // This one is a little unusual, since this is a single line we only
        // actually trim the start so we end up with a trailing space.
        Assert.Equal( "Test ", result );
    }

    [Fact]
    public void GetContent_IndentedNode_ReturnsTrimmedText()
    {
        var xml = "<summary>\r\n    Test\r\n    </summary>";
        var nav = GetNavigator( xml );

        var result = XmlComment.GetContent( nav );

        Assert.Equal( "Test", result );
    }

    #endregion

    #region GetPlainText

    [Fact]
    public void GetPlainText_EmptyContent_ReturnsEmptyString()
    {
        var xml = "<summary></summary>";
        var nav = GetNavigator( xml );

        var result = XmlComment.GetPlainText( nav );

        Assert.Equal( string.Empty, result );
    }

    [Fact]
    public void GetPlainText_PlainTextContent_ReturnsTrimmedString()
    {
        var xml = "<summary> Test </summary>";
        var nav = GetNavigator( xml );

        var result = XmlComment.GetPlainText( nav );

        // This one is a little unusual, since this is a single line we only
        // actually trim the start so we end up with a trailing space.
        Assert.Equal( "Test ", result );
    }

    [Fact]
    public void GetPlainText_MarkupContent_ReturnsPlainString()
    {
        var xml = "<summary>Te<b>s</b>t</summary>";
        var nav = GetNavigator( xml );

        var result = XmlComment.GetPlainText( nav );

        Assert.Equal( "Test", result );
    }

    [Fact]
    public void GetPlainText_CrefContent_ReturnsCrefString()
    {
        var xml = "<summary>Test <see cref=\"T:Data\"/></summary>";
        var nav = GetNavigator( xml );

        var result = XmlComment.GetPlainText( nav );

        Assert.Equal( "Test Data", result );
    }

    [Fact]
    public void GetPlainText_MultilineContent_ReturnsTrimmedString()
    {
        var xml = "<summary>\r\n    Te<b>s</b>t\r\n    </summary>";
        var nav = GetNavigator( xml );

        var result = XmlComment.GetPlainText( nav );

        Assert.Equal( "Test", result );
    }

    #endregion

    private static System.Xml.XPath.XPathNavigator? GetNavigator( string xml )
    {
        var doc = new System.Xml.XmlDocument
        {
            PreserveWhitespace = true
        };

        doc.LoadXml( xml );

        return doc.DocumentElement?.CreateNavigator();
    }
}
