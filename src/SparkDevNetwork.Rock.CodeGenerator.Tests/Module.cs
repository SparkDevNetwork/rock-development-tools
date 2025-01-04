namespace SparkDevNetwork.Rock.CodeGenerator.Tests;

public class Module
{
    [Fact]
    public Task VerifyChecks()
        => VerifyXunit.VerifyChecks.Run();
}