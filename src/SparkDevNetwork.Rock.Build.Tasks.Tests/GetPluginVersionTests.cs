using Microsoft.Build.Framework;

using Moq;

namespace SparkDevNetwork.Rock.Build.Tasks.Tests;

[TestClass]
public sealed class GetPluginVersionTests
{
    [TestMethod]
    public void EmptySourceSucceeds()
    {
        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );

        var buildTask = new GetPluginVersion
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            Source = ""
        };

        var success = buildTask.Execute();

        Assert.IsTrue( success );
        Assert.AreEqual( string.Empty, buildTask.Version );
    }

    [TestMethod]
    public void MissingSourceSucceeds()
    {
        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );

        fileSystem.Setup( m => m.FileExists( It.IsAny<string>() ) ).Returns( false );

        var buildTask = new GetPluginVersion
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            Source = "/missing-plugin.json"
        };

        var success = buildTask.Execute();

        Assert.IsTrue( success );
        Assert.AreEqual( string.Empty, buildTask.Version );
    }

    [TestMethod]
    public void MissingVersionInSourceSucceeds()
    {
        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );

        fileSystem.Setup( m => m.FileExists( It.IsAny<string>() ) ).Returns( true );
        fileSystem.Setup( m => m.ReadAllText( "/plugin.json" ) ).Returns( "{}" );

        var buildTask = new GetPluginVersion
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            Source = "/plugin.json"
        };

        var success = buildTask.Execute();

        Assert.IsTrue( success );
        Assert.AreEqual( string.Empty, buildTask.Version );
    }

    [TestMethod]
    public void OutputVersionMatchesSource()
    {
        var expectedVersion = "1.2.3-alpha.4";

        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );

        fileSystem.Setup( m => m.FileExists( It.IsAny<string>() ) ).Returns( true );
        fileSystem.Setup( m => m.ReadAllText( "/plugin.json" ) ).Returns( $"{{\"version\": \"{expectedVersion}\"}}" );

        var buildTask = new GetPluginVersion
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            Source = "/plugin.json"
        };

        var success = buildTask.Execute();

        Assert.IsTrue( success );
        Assert.AreEqual( expectedVersion, buildTask.Version );
    }

    [TestMethod]
    public void InvalidSourceFails()
    {
        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );

        fileSystem.Setup( m => m.FileExists( It.IsAny<string>() ) ).Returns( true );
        fileSystem.Setup( m => m.ReadAllText( "/plugin.json" ) ).Returns( "foobar" );

        var buildTask = new GetPluginVersion
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            Source = "/plugin.json"
        };

        var success = buildTask.Execute();

        Assert.IsFalse( success );
        Assert.AreEqual( string.Empty, buildTask.Version );
    }
}
