using Moq;

using SparkDevNetwork.Rock.CodeGenerator.Documentation;

namespace SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation;

public class XmlCommentSetTests
{
    [Fact]
    public void Constructor_SetsPropertyValues()
    {
        var summaryMock = new Mock<IComment>();
        var valueMock = new Mock<IComment>();
        var inheritFrom = "T:Test";

        var set = new XmlCommentSet( summaryMock.Object, valueMock.Object, inheritFrom );

        Assert.Same( summaryMock.Object, set.Summary );
        Assert.Same( valueMock.Object, set.Value );
        Assert.Equal( inheritFrom, set.InheritFrom );
    }
}
