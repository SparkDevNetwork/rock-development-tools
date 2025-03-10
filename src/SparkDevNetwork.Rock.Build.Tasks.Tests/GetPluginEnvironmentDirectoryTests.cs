using Microsoft.Build.Framework;

using Moq;

namespace SparkDevNetwork.Rock.Build.Tasks.Tests;

public sealed class GetPluginEnvironmentDirectoryTests
{
    [Fact]
    public void EmptyConfigurationFileSucceeds()
    {
        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );

        var buildTask = new GetPluginEnvironmentDirectory
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            PluginConfigurationFile = ""
        };

        var success = buildTask.Execute();

        Assert.True( success );
        Assert.Equal( string.Empty, buildTask.Path );
    }

    [Fact]
    public void MissingConfigurationFileSucceeds()
    {
        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );

        fileSystem.Setup( m => m.FileExists( It.IsAny<string>() ) ).Returns( false );

        var buildTask = new GetPluginEnvironmentDirectory
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            PluginConfigurationFile = "/missing-plugin.json"
        };

        var success = buildTask.Execute();

        Assert.True( success );
        Assert.Equal( string.Empty, buildTask.Path );
    }

    [Fact]
    public void MissingEnvironmentReturnsEmptyPath()
    {
        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );

        fileSystem.Setup( m => m.PathCombine( It.IsAny<string>(), "environment.json" ) ).Returns<string, string>( ( a, b ) => $"{(a == "/" ? "" : a)}/{b}" );
        fileSystem.Setup( m => m.GetFullPath( "/env/plugin/csharp/plugin.json" ) ).Returns( "/env/plugin/csharp/plugin.json" );
        fileSystem.Setup( m => m.GetDirectoryName( "/env/plugin/csharp/plugin.json" ) ).Returns( "/env/plugin/csharp" );
        fileSystem.Setup( m => m.GetDirectoryName( "/env/plugin/csharp" ) ).Returns( "/env/plugin" );
        fileSystem.Setup( m => m.GetDirectoryName( "/env/plugin" ) ).Returns( "/env" );
        fileSystem.Setup( m => m.GetDirectoryName( "/env" ) ).Returns( "/" );
        fileSystem.Setup( m => m.GetDirectoryName( "/" ) ).Returns( ( string ) null! );
        fileSystem.Setup( m => m.FileExists( "/env/plugin/csharp/plugin.json" ) ).Returns( true );
        fileSystem.Setup( m => m.FileExists( "/env/plugin/csharp/environment.json" ) ).Returns( false );
        fileSystem.Setup( m => m.FileExists( "/env/plugin/environment.json" ) ).Returns( false );
        fileSystem.Setup( m => m.FileExists( "/env/environment.json" ) ).Returns( false );
        fileSystem.Setup( m => m.FileExists( "/environment.json" ) ).Returns( false );

        var buildTask = new GetPluginEnvironmentDirectory
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            PluginConfigurationFile = "/env/plugin/csharp/plugin.json"
        };

        var success = buildTask.Execute();

        Assert.True( success );
        Assert.Equal( string.Empty, buildTask.Path );
    }

    [Fact]
    public void ValidEnvironmentReturnsPath()
    {
        var expectedPath = "/env";

        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );

        fileSystem.Setup( m => m.PathCombine( It.IsAny<string>(), "environment.json" ) ).Returns<string, string>( ( a, b ) => $"{(a == "/" ? "" : a)}/{b}" );
        fileSystem.Setup( m => m.GetFullPath( "/env/plugin/csharp/plugin.json" ) ).Returns( "/env/plugin/csharp/plugin.json" );
        fileSystem.Setup( m => m.GetDirectoryName( "/env/plugin/csharp/plugin.json" ) ).Returns( "/env/plugin/csharp" );
        fileSystem.Setup( m => m.GetDirectoryName( "/env/plugin/csharp" ) ).Returns( "/env/plugin" );
        fileSystem.Setup( m => m.GetDirectoryName( "/env/plugin" ) ).Returns( "/env" );
        fileSystem.Setup( m => m.GetDirectoryName( "/env" ) ).Returns( "/" );
        fileSystem.Setup( m => m.FileExists( "/env/plugin/csharp/plugin.json" ) ).Returns( true );
        fileSystem.Setup( m => m.FileExists( "/env/plugin/csharp/environment.json" ) ).Returns( false );
        fileSystem.Setup( m => m.FileExists( "/env/plugin/environment.json" ) ).Returns( false );
        fileSystem.Setup( m => m.FileExists( "/env/environment.json" ) ).Returns( true );

        var buildTask = new GetPluginEnvironmentDirectory
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            PluginConfigurationFile = "/env/plugin/csharp/plugin.json"
        };

        var success = buildTask.Execute();

        Assert.True( success );
        Assert.Equal( expectedPath, buildTask.Path );
    }
}
