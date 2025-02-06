namespace SparkDevNetwork.Rock.CodeGenerator.Tests;

public class IndentedStringBuilderTests
{
    [Fact]
    public void IndentsByCorrectAmount()
    {
        var expectedText = "        indented text" + Environment.NewLine;
        var builder = new IndentedStringBuilder( 0, 4 );

        builder.Indent();
        builder.Indent();
        builder.AppendLine( "indented text" );

        var text = builder.ToString();

        Assert.Equal( expectedText, text );
    }

    [Fact]
    public void UnindentsByCorrectAmount()
    {
        var expectedText = "    indented text" + Environment.NewLine;
        var builder = new IndentedStringBuilder( 0, 4 );

        builder.Indent();
        builder.Indent();
        builder.Unindent();
        builder.AppendLine( "indented text" );

        var text = builder.ToString();

        Assert.Equal( expectedText, text );
    }

    [Fact]
    public void IndentWithDelegateDecreasedIndentationAfterCall()
    {
        var expectedText = "    indented text" + Environment.NewLine + "unindented text" + Environment.NewLine;
        var builder = new IndentedStringBuilder( 0, 4 );

        builder.Indent( () => builder.AppendLine( "indented text" ) );
        builder.AppendLine( "unindented text" );

        var text = builder.ToString();

        Assert.Equal( expectedText, text );
    }

    [Fact]
    public void EmptyAppendLineDoesNotIndent()
    {
        var expectedText = Environment.NewLine;
        var builder = new IndentedStringBuilder( 0, 4 );

        builder.Indent();
        builder.AppendLine();

        var text = builder.ToString();

        Assert.Equal( expectedText, text );
    }

    [Fact]
    public void UnindentThrowsWhenCalledTooManyTimes()
    {
        var builder = new IndentedStringBuilder( 1, 4 );

        builder.Unindent();
        Assert.Throws<InvalidOperationException>( () => builder.Unindent() );
    }
}
