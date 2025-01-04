using Microsoft.Build.Framework;

using Moq;

namespace SparkDevNetwork.Rock.Build.Tasks.Tests;

public sealed class GetPluginVersionTests
{
    [Fact]
    public void EmptySourceSucceeds()
    {
        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );

        var buildTask = new GetPluginVersion
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            PluginConfigurationFile = ""
        };

        var success = buildTask.Execute();

        Assert.True( success );
        Assert.Equal( string.Empty, buildTask.Version );
    }

    [Fact]
    public void MissingSourceSucceeds()
    {
        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );

        fileSystem.Setup( m => m.FileExists( It.IsAny<string>() ) ).Returns( false );

        var buildTask = new GetPluginVersion
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            PluginConfigurationFile = "/missing-plugin.json"
        };

        var success = buildTask.Execute();

        Assert.True( success );
        Assert.Equal( string.Empty, buildTask.Version );
    }

    [Fact]
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
            PluginConfigurationFile = "/plugin.json"
        };

        var success = buildTask.Execute();

        Assert.True( success );
        Assert.Equal( string.Empty, buildTask.Version );
    }

    [Fact]
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
            PluginConfigurationFile = "/plugin.json"
        };

        var success = buildTask.Execute();

        Assert.True( success );
        Assert.Equal( expectedVersion, buildTask.Version );
    }

    [Fact]
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
            PluginConfigurationFile = "/plugin.json"
        };

        var success = buildTask.Execute();

        Assert.False( success );
        Assert.Equal( string.Empty, buildTask.Version );
    }
}
