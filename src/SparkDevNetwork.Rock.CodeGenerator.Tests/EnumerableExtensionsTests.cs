namespace SparkDevNetwork.Rock.CodeGenerator.Tests;

public class EnumerableExtensionsTests
{
    [Fact]
    public void TakeLast_WhenCalled_ReturnsLastNItems()
    {
        var source = new List<int> { 1, 2, 3, 4, 5 };

        var result = source.TakeLast( 3 );

        Assert.Equal( [3, 4, 5], result );
    }
}
