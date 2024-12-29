namespace SparkDevNetwork.Rock.CodeGenerator.Tests;

public class StringExtensionsTests
{
    #region SplitCase

    [Fact]
    public void SplitCase_WithNull_ReturnsNull()
    {
        var str = ( string? ) null;

        var result = str.SplitCase();

        Assert.Null( result );
    }

    [Fact]
    public void SplitCase_WithLowercaseString_ReturnsOriginalString()
    {
        var original = "thisislowercase";

        var result = original.SplitCase();

        Assert.Equal( original, result );
    }

    [Fact]
    public void SplitCase_WithUppercaseString_ReturnsOriginalString()
    {
        var original = "THISISUPPERCASE";

        var result = original.SplitCase();

        Assert.Equal( original, result );
    }

    [Fact]
    public void SplitCase_WithCamelCaseString_ReturnsIndividualWords()
    {
        var original = "thisIsCamelCase";

        var result = original.SplitCase();

        Assert.Equal( "this Is Camel Case", result );
    }


    [Fact]
    public void SplitCase_WithPascalCaseString_ReturnsIndividualWords()
    {
        var original = "ThisIsCamelCase";

        var result = original.SplitCase();

        Assert.Equal( "This Is Camel Case", result );
    }

    #endregion

    #region SubstringSafe

    [Fact]
    public void SubstringSafe_WithNullAndNoLength_ReturnsEmptyString()
    {
        var original = ( string? ) null;

        var result = original.SubstringSafe( 3 );

        Assert.Equal( string.Empty, result );
    }

    [Fact]
    public void SubstringSafe_WithInvalidStartIndexAndNoLength_ReturnsEmptyString()
    {
        var original = "12";

        var result = original.SubstringSafe( 3 );

        Assert.Equal( string.Empty, result );
    }

    [Fact]
    public void SubstringSafe_WithNull_ReturnsEmptyString()
    {
        var original = ( string? ) null;

        var result = original.SubstringSafe( 3, 2 );

        Assert.Equal( string.Empty, result );
    }

    [Fact]
    public void SubstringSafe_WithInvalidStartIndex_ReturnsEmptyString()
    {
        var original = "12";

        var result = original.SubstringSafe( 3, 2 );

        Assert.Equal( string.Empty, result );
    }

    [Fact]
    public void SubstringSafe_WithNegativeStartIndex_ReturnsEmptyString()
    {
        var original = "12";

        var result = original.SubstringSafe( -1, 2 );

        Assert.Equal( string.Empty, result );
    }

    [Fact]
    public void SubstringSafe_WithNegativeMaxLength_ReturnsEmptyString()
    {
        var original = "12";

        var result = original.SubstringSafe( 0, -1 );

        Assert.Equal( string.Empty, result );
    }

    [Fact]
    public void SubstringSafe_WithMaxLengthTooBig_ReturnsCorrectString()
    {
        var original = "12345";

        var result = original.SubstringSafe( 3, 5 );

        Assert.Equal( "45", result );
    }

    [Fact]
    public void SubstringSafe_WithShortMaxLength_ReturnsCorrectString()
    {
        var original = "12345";

        var result = original.SubstringSafe( 3, 1 );

        Assert.Equal( "4", result );
    }

    #endregion

    #region ToCamelCase

    [Fact]
    public void ToCamelCase_WithNull_ReturnsNull()
    {
        var original = ( string? ) null;

        var result = original.ToCamelCase();

        Assert.Null( result );
    }

    [Fact]
    public void ToCamelCase_WithLowercaseFirstCharacter_ReturnsOriginalString()
    {
        var original = "alreadyCamelCase";

        var result = original.ToCamelCase();

        Assert.Equal( original, result );
    }

    [Theory]
    [InlineData( "ThisIsPascalCase", "thisIsPascalCase" )]
    [InlineData( "ThisIs PascalCase", "thisIs PascalCase" )]
    [InlineData( "FOO bar", "foo bar" )]
    [InlineData( "FOOBar", "fooBar" )]
    public void ToCamelCase_WithString_ReturnsCamelCaseString( string original, string expected )
    {
        var result = original.ToCamelCase();

        Assert.Equal( expected, result );
    }

    #endregion

    #region ToStringSafe
    
    [Fact]
    public void ToStringSafe_ReturnsEmptyStringFromNull()
    {
        object? nullObj = null;

        var value = nullObj.ToStringSafe();

        Assert.Equal( string.Empty, value );
    }

    [Fact]
    public void ToStringSafe_ReturnsValueAsStringFromObject()
    {
        var value = 42.ToStringSafe();

        Assert.Equal( "42", value );
    }

    #endregion
}
