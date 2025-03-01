using Moq;

namespace SparkDevNetwork.Rock.CodeGenerator.Tests;

public class GeneratorTests
{
    #region GenerateCSharpFile

    [Fact]
    public void GenerateCSharpFile_WithAutoGenAndAutoGeneratedComment_IncludesComment()
    {
        var generator = new TestGenerator();
        var usings = new List<string>();
        var namespaceName = "TestNamespace";
        var body = "public class TestClass { }";
        var generatedComment = "Auto-generated";

        var stringsProviderMock = new Mock<IGeneratorStringsProvider>();
        stringsProviderMock.SetupGet( s => s.AutoGeneratedComment ).Returns( generatedComment );

        generator.StringsProvider = stringsProviderMock.Object;

        var result = generator.CallGenerateCSharpFile( usings, namespaceName, body, true );

        Assert.Contains( generatedComment, result );
    }

    [Fact]
    public void GenerateCSharpFile_WithAutoGenButNoString_Succeeds()
    {
        var generator = new TestGenerator();
        var usings = new List<string>();
        var namespaceName = "TestNamespace";
        var body = "public class TestClass { }";

        var stringsProviderMock = new Mock<IGeneratorStringsProvider>();

        generator.StringsProvider = stringsProviderMock.Object;

        var result = generator.CallGenerateCSharpFile( usings, namespaceName, body, true );

        Assert.NotNull( result );
    }

    [Fact]
    public void GenerateCSharpFile_WithAutoGenButProvider_Succeeds()
    {
        var generator = new TestGenerator();
        var usings = new List<string>();
        var namespaceName = "TestNamespace";
        var body = "public class TestClass { }";

        var result = generator.CallGenerateCSharpFile( usings, namespaceName, body, true );

        Assert.NotNull( result );
    }

    [Fact]
    public void GenerateCSharpFile_WithCopyrightComment_IncludesComment()
    {
        var generator = new TestGenerator();
        var usings = new List<string>();
        var namespaceName = "TestNamespace";
        var body = "public class TestClass { }";
        var generatedComment = "copyright-generated";

        var stringsProviderMock = new Mock<IGeneratorStringsProvider>();
        stringsProviderMock.SetupGet( s => s.CopyrightComment ).Returns( generatedComment );

        generator.StringsProvider = stringsProviderMock.Object;

        var result = generator.CallGenerateCSharpFile( usings, namespaceName, body, true );

        Assert.Contains( generatedComment, result );
    }

    #endregion

    #region GenerateTypeScriptFile

    [Fact]
    public void GenerateTypeScriptFile_WithAutoGenAndAutoGeneratedComment_IncludesComment()
    {
        var generator = new TestGenerator();
        var usings = new List<TypeScriptImport>();
        var body = "public class TestClass { }";
        var generatedComment = "Auto-generated";

        var stringsProviderMock = new Mock<IGeneratorStringsProvider>();
        stringsProviderMock.SetupGet( s => s.AutoGeneratedComment ).Returns( generatedComment );

        generator.StringsProvider = stringsProviderMock.Object;

        var result = generator.CallGenerateTypeScriptFile( usings, body, true );

        Assert.Contains( generatedComment, result );
    }

    [Fact]
    public void GenerateTypeScriptFile_WithAutoGenButNoString_Succeeds()
    {
        var generator = new TestGenerator();
        var usings = new List<TypeScriptImport>();
        var body = "public class TestClass { }";

        var stringsProviderMock = new Mock<IGeneratorStringsProvider>();

        generator.StringsProvider = stringsProviderMock.Object;

        var result = generator.CallGenerateTypeScriptFile( usings, body, true );

        Assert.NotNull( result );
    }

    [Fact]
    public void GenerateTypeScriptFile_WithAutoGenButProvider_Succeeds()
    {
        var generator = new TestGenerator();
        var usings = new List<TypeScriptImport>();
        var body = "public class TestClass { }";

        var result = generator.CallGenerateTypeScriptFile( usings, body, true );

        Assert.NotNull( result );
    }

    [Fact]
    public void GenerateTypeScriptFile_WithCopyrightComment_IncludesComment()
    {
        var generator = new TestGenerator();
        var usings = new List<TypeScriptImport>();
        var body = "public class TestClass { }";
        var generatedComment = "copyright-generated";

        var stringsProviderMock = new Mock<IGeneratorStringsProvider>();
        stringsProviderMock.SetupGet( s => s.CopyrightComment ).Returns( generatedComment );

        generator.StringsProvider = stringsProviderMock.Object;

        var result = generator.CallGenerateTypeScriptFile( usings, body, true );

        Assert.Contains( generatedComment, result );
    }

    #endregion

    #region GenerateCSharpUsings

    [Fact]
    public void GenerateCSharpUsings_SortsSystemFirst()
    {
        var generator = new TestGenerator();
        var usings = new List<string>
        {
            "Alpha",
            "System.Linq"
        };

        var code = generator.CallGenerateCSharpUsings( usings );

        Assert.NotNull( code );

        var systemLinqIndex = code.IndexOf( "using System.Linq" );
        var alphaIndex = code.IndexOf( "using Alpha" );

        Assert.NotEqual( -1, systemLinqIndex );
        Assert.NotEqual( -1, alphaIndex );

        Assert.True( systemLinqIndex < alphaIndex );
    }

    [Fact]
    public void GenerateCSharpUsings_SeparatesGroupingsByBlankLine()
    {
        var generator = new TestGenerator();
        var usings = new List<string>
        {
            "Alpha",
            "Beta"
        };
        var expectedString = "using Alpha;\n\nusing Beta;";

        var code = generator.CallGenerateCSharpUsings( usings )
            ?.Replace( "\r\n", "\n" );

        Assert.NotNull( code );
        Assert.Contains( expectedString, code );
    }

    #endregion

    #region GenerateTypeScriptImports

    [Fact]
    public void GenerateTypeScriptImports_PlacesRelativeImportsLast()
    {
        var generator = new TestGenerator();
        var imports = new List<TypeScriptImport>
        {
            new TypeScriptImport
            {
                DefaultImport = "alpha",
                SourcePath = "../alpha"
            },
            new TypeScriptImport
            {
                DefaultImport = "beta",
                SourcePath = "beta"
            }
        };

        var code = generator.CallGenerateTypeScriptImports( imports );

        Assert.NotNull( code );

        var alphaIndex = code.IndexOf( "import alpha" );
        var betaIndex = code.IndexOf( "import beta" );

        Assert.NotEqual( -1, alphaIndex );
        Assert.NotEqual( -1, betaIndex );
        Assert.True( alphaIndex > betaIndex );
    }

    [Fact]
    public void GenerateTypeScriptImports_SortsImportsAlpabetically()
    {
        var generator = new TestGenerator();
        var imports = new List<TypeScriptImport>
        {
            new TypeScriptImport
            {
                DefaultImport = "beta",
                SourcePath = "beta"
            },
            new TypeScriptImport
            {
                DefaultImport = "alpha",
                SourcePath = "alpha"
            }
        };

        var code = generator.CallGenerateTypeScriptImports( imports );

        Assert.NotNull( code );

        var alphaIndex = code.IndexOf( "import alpha" );
        var betaIndex = code.IndexOf( "import beta" );

        Assert.NotEqual( -1, alphaIndex );
        Assert.NotEqual( -1, betaIndex );
        Assert.True( alphaIndex < betaIndex );
    }

    [Fact]
    public void GenerateTypeScriptImports_GroupsNamedImports()
    {
        var generator = new TestGenerator();
        var imports = new List<TypeScriptImport>
        {
            new TypeScriptImport
            {
                DefaultImport = "alpha",
                SourcePath = "alpha"
            },
            new TypeScriptImport
            {
                NamedImport = "beta",
                SourcePath = "alpha"
            }
        };
        var expectedImport = "import alpha, { beta } from \"alpha\"";

        var code = generator.CallGenerateTypeScriptImports( imports );

        Assert.NotNull( code );
        Assert.Contains( expectedImport, code );
    }

    #endregion

    #region Support Classes

    private class TestGenerator : Generator
    {
        public string CallGenerateCSharpFile( IList<string> usings, string namespaceName, string body, bool isAutoGen )
        {
            return GenerateCSharpFile( usings, namespaceName, body, isAutoGen );
        }

        public string CallGenerateCSharpUsings( IList<string> usings )
        {
            return GenerateCSharpUsings( usings );
        }

        public string CallGenerateTypeScriptFile( IList<TypeScriptImport> imports, string body, bool isAutoGen )
        {
            return GenerateTypeScriptFile( imports, body, isAutoGen );
        }

        public string CallGenerateTypeScriptImports( IList<TypeScriptImport> imports )
        {
            return GenerateTypeScriptImports( imports );
        }
    }

    #endregion
}
