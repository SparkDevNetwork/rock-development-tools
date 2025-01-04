using Microsoft.Build.Framework;

using Moq;

namespace SparkDevNetwork.Rock.Build.Tasks.Tests;

public sealed class CopyAssemblyToRockWebTests
{
    [Fact]
    public void EmptySourceFails()
    {
        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );
        var errors = new List<string?>();

        buildEngine.Setup( m => m.LogErrorEvent( It.IsAny<BuildErrorEventArgs>() ) )
            .Callback<BuildErrorEventArgs>( args => errors.Add( args.Message ) );

        var buildTask = new CopyAssemblyToRockWeb
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            Source = ""
        };

        var success = buildTask.Execute();

        Assert.False( success );
        Assert.Single( errors );
        Assert.Equal( "The Source property must be set.", errors[0] );
    }

    [Fact]
    public void EmptyDestinationFails()
    {
        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );
        var errors = new List<string?>();

        buildEngine.Setup( m => m.LogErrorEvent( It.IsAny<BuildErrorEventArgs>() ) )
            .Callback<BuildErrorEventArgs>( args => errors.Add( args.Message ) );

        var buildTask = new CopyAssemblyToRockWeb
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            Source = "exists",
            Destination = ""
        };

        var success = buildTask.Execute();

        Assert.False( success );
        Assert.Single( errors );
        Assert.Equal( "The Destination property must be set.", errors[0] );
    }

    [Fact]
    public void MissingAssemblyFileFails()
    {
        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );
        var errors = new List<string?>();

        buildEngine.Setup( m => m.LogErrorEvent( It.IsAny<BuildErrorEventArgs>() ) )
            .Callback<BuildErrorEventArgs>( args => errors.Add( args.Message ) );

        fileSystem.Setup( m => m.PathCombine( It.IsAny<string>(), It.IsAny<string>() ) )
            .Returns<string, string>( ( a, b ) => $"{a}/{b}" );
        fileSystem.Setup( m => m.FileExists( "src/missing.dll" ) ).Returns( false );

        var buildTask = new CopyAssemblyToRockWeb
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            Source = "src",
            Destination = "dest",
            Files = "missing.dll"
        };

        var success = buildTask.Execute();

        Assert.False( success );
        Assert.Single( errors );
        Assert.Equal( "The file src/missing.dll does not exist.", errors[0] );
    }

    [Fact]
    public void CopiesAssemblyFile()
    {
        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );

        fileSystem.Setup( m => m.PathCombine( It.IsAny<string>(), It.IsAny<string>() ) )
            .Returns<string, string>( ( a, b ) => $"{a}/{b}" );
        fileSystem.Setup( m => m.GetFullPath( "dest" ) ).Returns( "dest" );
        fileSystem.Setup( m => m.GetFileName( "src/library.dll" ) ).Returns( "library.dll" );
        fileSystem.Setup( m => m.FileExists( "src/library.dll" ) ).Returns( true );
        fileSystem.Setup( m => m.FileExists( "src/library.pdb" ) ).Returns( false );
        fileSystem.Setup( m => m.FileCopy( "src/library.dll", "dest/library.dll", It.IsAny<bool>() ) )
            .Verifiable( Times.Once() );

        var buildTask = new CopyAssemblyToRockWeb
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            Source = "src",
            Destination = "dest",
            Files = "library.dll"
        };

        var success = buildTask.Execute();

        Assert.True( success );
        fileSystem.VerifyAll();
    }

    [Fact]
    public void CopiesPdbFile()
    {
        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );

        fileSystem.Setup( m => m.PathCombine( It.IsAny<string>(), It.IsAny<string>() ) )
            .Returns<string, string>( ( a, b ) => $"{a}/{b}" );
        fileSystem.Setup( m => m.GetFullPath( "dest" ) ).Returns( "dest" );
        fileSystem.Setup( m => m.GetFileName( "src/library.dll" ) ).Returns( "library.dll" );
        fileSystem.Setup( m => m.GetFileName( "src/library.pdb" ) ).Returns( "library.pdb" );
        fileSystem.Setup( m => m.FileExists( "src/library.dll" ) ).Returns( true );
        fileSystem.Setup( m => m.FileExists( "src/library.pdb" ) ).Returns( true );
        fileSystem.Setup( m => m.FileCopy( "src/library.dll", "dest/library.dll", It.IsAny<bool>() ) );
        fileSystem.Setup( m => m.FileCopy( "src/library.pdb", "dest/library.pdb", It.IsAny<bool>() ) )
            .Verifiable( Times.Once() );

        var buildTask = new CopyAssemblyToRockWeb
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            Source = "src",
            Destination = "dest",
            Files = "library.dll"
        };

        var success = buildTask.Execute();

        Assert.True( success );
        fileSystem.VerifyAll();
    }

    [Fact]
    public void CopiesAssemblyFileWithoutExtension()
    {
        var buildEngine = new Mock<IBuildEngine>();
        var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );

        fileSystem.Setup( m => m.PathCombine( It.IsAny<string>(), It.IsAny<string>() ) )
            .Returns<string, string>( ( a, b ) => $"{a}/{b}" );
        fileSystem.Setup( m => m.GetFullPath( "dest" ) ).Returns( "dest" );
        fileSystem.Setup( m => m.GetFileName( "src/library.dll" ) ).Returns( "library.dll" );
        fileSystem.Setup( m => m.FileExists( "src/library.dll" ) ).Returns( true );
        fileSystem.Setup( m => m.FileExists( "src/library.pdb" ) ).Returns( false );
        fileSystem.Setup( m => m.FileCopy( "src/library.dll", "dest/library.dll", It.IsAny<bool>() ) )
            .Verifiable( Times.Once() );

        var buildTask = new CopyAssemblyToRockWeb
        {
            FileSystem = fileSystem.Object,
            BuildEngine = buildEngine.Object,
            Source = "src",
            Destination = "dest",
            Files = "library"
        };

        var success = buildTask.Execute();

        Assert.True( success );
        fileSystem.VerifyAll();
    }
}
