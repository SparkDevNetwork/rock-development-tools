using SparkDevNetwork.Rock.CodeGenerator;
using SparkDevNetwork.Rock.DevTool.Generators;

using Xunit.Abstractions;
using Xunit.Sdk;

namespace SparkDevNetwork.Rock.DevTool.Tests;

public class PluginTypeScriptGeneratorTests
{
    #region GetPathReferenceComponents

    [Theory]
    [MemberData( nameof( GetPathMappingTestData ) )]
    public void GetPathReferenceComponents_WithPaths_MapsCorrectly( PathReferenceData data )
    {
        var sourcePathComponents = data.Source.ToList();
        var targetPathComponents = data.Target.ToList();
        var expectedComponents = data.Expected.ToList();

        var actualComponents = PluginTypeScriptGenerator.GetPathReferenceComponents( sourcePathComponents, targetPathComponents );

        if ( expectedComponents.Count != actualComponents.Count )
        {
            throw new XunitException( $"Expected '{string.Join( "/", expectedComponents )}' but got '{string.Join( "/", actualComponents )}'" );
        }

        if ( !expectedComponents.SequenceEqual( actualComponents ) )
        {
            throw new XunitException( $"Expected '{string.Join( "/", expectedComponents )}' but got '{string.Join( "/", actualComponents )}'" );
        }
    }

    /// <summary>
    /// Gets the test data for <see cref="GetPathReferenceComponents_WithPaths_MapsCorrectly"/>.
    /// </summary>
    /// <returns>A list of test data rows.</returns>
    public static object[][] GetPathMappingTestData()
    {
        // Simulated directory structure:
        // enums.partial.ts
        // Enums/codes.partial.ts
        // viewModels.d.ts
        // ViewModels/subFolder.d.ts
        // ViewModels/SubFolder/deep.d.ts
        // ViewModels/SubFolder/Deep/superDeep.d.ts
        // ViewModels/SubFolder2/deep2.d.ts

        return
        [
            [new PathReferenceData(["ViewModels"], ["ViewModels", "SubFolder"], [".", "ViewModels", "subFolder"])],
            [new PathReferenceData(["ViewModels"], ["ViewModels", "SubFolder", "Deep"], [".", "ViewModels", "SubFolder", "deep"])],
            [new PathReferenceData(["ViewModels", "SubFolder"], ["ViewModels", "SubFolder", "Deep"], [".", "deep"])],
            [new PathReferenceData(["ViewModels", "SubFolder"], ["ViewModels"], ["..", "viewModels"])],
            [new PathReferenceData(["ViewModels", "SubFolder", "Deep"], ["ViewModels", "SubFolder"], ["..", "subFolder"])],
            [new PathReferenceData(["ViewModels", "SubFolder", "Deep"], ["ViewModels"], ["..", "..", "viewModels"])],
            [new PathReferenceData(["ViewModels", "SubFolder", "Deep", "SuperDeep"], ["ViewModels", "SubFolder"], ["..", "..", "subFolder"])],
            [new PathReferenceData(["ViewModels", "SubFolder2", "Deep2"], ["ViewModels", "SubFolder", "Deep"], ["..", "SubFolder", "deep"])],
            [new PathReferenceData(["ViewModels"], ["Enums"], [".", "enums"])],
            [new PathReferenceData(["ViewModels"], ["Enums", "Codes"], [".", "Enums", "codes"])],
            [new PathReferenceData(["ViewModels", "SubFolder"], ["Enums"], ["..", "enums"])],
            [new PathReferenceData(["ViewModels", "SubFolder"], ["Enums", "Codes"], ["..", "Enums", "codes"])],
        ];
    }

    [Fact]
    public void GetPathReferenceComponents_WithSamePath_ThrowsException()
    {
        List<string> sourcePathComponents = ["ViewModels"];
        List<string> targetPathComponents = ["ViewModels"];

        Assert.Throws<ArgumentException>( () =>
        {
            PluginTypeScriptGenerator.GetPathReferenceComponents( sourcePathComponents, targetPathComponents );
        } );
    }

    public class PathReferenceData : IXunitSerializable
    {
        public string[] Source { get; private set; } = [];

        public string[] Target { get; private set; } = [];

        public string[] Expected { get; private set; } = [];

        public PathReferenceData()
        {
        }

        public PathReferenceData( string[] source, string[] target, string[] expected )
        {
            Source = source;
            Target = target;
            Expected = expected;
        }

        public void Deserialize( IXunitSerializationInfo info )
        {
            Source = info.GetValue<string[]>( "source" );
            Target = info.GetValue<string[]>( "target" );
            Expected = info.GetValue<string[]>( "expected" );
        }

        public void Serialize( IXunitSerializationInfo info )
        {
            info.AddValue( "source", Source );
            info.AddValue( "target", Target );
            info.AddValue( "expected", Expected );
        }

        public override string ToString()
        {
            return $"{string.Join( '.', Source )} => {string.Join( '.', Target )}";
        }
    }

    #endregion

    #region GetTypeScriptTypeDefinition

    [Fact]
    public void GetTypeScriptTypeDefinition_WithBag_ReturnsImport()
    {
        var generator = new PluginTypeScriptGeneratorTester();

        var definition = generator.CallGetTypeScriptTypeDefinition( typeof( CodeGenerator.Tests.Sample.ViewModels.FirstBag ), false );

        Assert.NotNull( definition );
        Assert.Single( definition.Imports );
        Assert.Equal( "FirstBag", definition.Imports.First().NamedImport );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_WithBag_ReturnsImportPath()
    {
        var generator = new PluginTypeScriptGeneratorTester();

        var definition = generator.CallGetTypeScriptTypeDefinition( typeof( CodeGenerator.Tests.Sample.ViewModels.SubFolder.ThirdBag ), false );

        Assert.NotNull( definition );
        Assert.Single( definition.Imports );
        Assert.Equal( "./ViewModels/subFolder", definition.Imports.First().SourcePath );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_WithBox_FindsType()
    {
        var generator = new PluginTypeScriptGeneratorTester();

        var definition = generator.CallGetTypeScriptTypeDefinition( typeof( CodeGenerator.Tests.Sample.ViewModels.FirstBox ), false );

        Assert.NotNull( definition );
        Assert.Single( definition.Imports );
        Assert.Equal( "FirstBox", definition.Imports.First().NamedImport );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_WithEnum_FindsType()
    {
        var generator = new PluginTypeScriptGeneratorTester();

        var definition = generator.CallGetTypeScriptTypeDefinition( typeof( CodeGenerator.Tests.Sample.Enums.Status ), true );

        Assert.NotNull( definition );
        Assert.Single( definition.Imports );
        Assert.Equal( "Status", definition.Imports.First().NamedImport );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_WithBagInWrongNamespace_ConvertsToUnknown()
    {
        var generator = new PluginTypeScriptGeneratorTester();

        var definition = generator.CallGetTypeScriptTypeDefinition( typeof( CodeGenerator.Tests.Sample.NotValidBag ), false );

        Assert.NotNull( definition );
        Assert.Empty( definition.Imports );
        Assert.Equal( "unknown", definition.Definition );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_WithBagInNoNamespace_ConvertsToUnknown()
    {
        var generator = new PluginTypeScriptGeneratorTester();

        var definition = generator.CallGetTypeScriptTypeDefinition( typeof( NoNamespaceBag ), false );

        Assert.NotNull( definition );
        Assert.Empty( definition.Imports );
        Assert.Equal( "unknown", definition.Definition );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_WithEnumInWrongNamespace_ConvertsToNumber()
    {
        var generator = new PluginTypeScriptGeneratorTester();

        var definition = generator.CallGetTypeScriptTypeDefinition( typeof( CodeGenerator.Tests.Sample.NotValidEnum ), true );

        Assert.NotNull( definition );
        Assert.Empty( definition.Imports );
        Assert.Equal( "number", definition.Definition );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_WithOptionalBag_DefinesTypeAsNullable()
    {
        var generator = new PluginTypeScriptGeneratorTester();

        var definition = generator.CallGetTypeScriptTypeDefinition( typeof( CodeGenerator.Tests.Sample.ViewModels.FirstBag ), false );

        Assert.NotNull( definition );
        Assert.Equal( "FirstBag | null", definition.Definition );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_WithRequiredBag_DefinesTypeOnly()
    {
        var generator = new PluginTypeScriptGeneratorTester();

        var definition = generator.CallGetTypeScriptTypeDefinition( typeof( CodeGenerator.Tests.Sample.ViewModels.FirstBag ), true );

        Assert.NotNull( definition );
        Assert.Equal( "FirstBag", definition.Definition );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_WithBagCollection_DefinesTypeAsCollection()
    {
        var generator = new PluginTypeScriptGeneratorTester();

        var definition = generator.CallGetTypeScriptTypeDefinition( typeof( List<CodeGenerator.Tests.Sample.ViewModels.FirstBag> ), false );

        Assert.NotNull( definition );
        Assert.Equal( "FirstBag[] | null", definition.Definition );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_WithRequiredGuid_DefinesTypeAsGuid()
    {
        var generator = new PluginTypeScriptGeneratorTester();

        var definition = generator.CallGetTypeScriptTypeDefinition( typeof( Guid ), true );

        Assert.NotNull( definition );
        Assert.Equal( "Guid", definition.Definition );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_WithOptionalGuid_DefinesTypeAsNullable()
    {
        var generator = new PluginTypeScriptGeneratorTester();

        var definition = generator.CallGetTypeScriptTypeDefinition( typeof( Guid? ), false );

        Assert.NotNull( definition );
        Assert.Equal( "Guid | null", definition.Definition );
    }

    private class PluginTypeScriptGeneratorTester : PluginTypeScriptGenerator
    {
        public PluginTypeScriptGeneratorTester()
            : base( ["src"] )
        {
        }

        public TypeScriptTypeDefinition? CallGetTypeScriptTypeDefinition( Type type, bool isRequired )
        {
            return GetTypeScriptTypeDefinition( type, isRequired );
        }
    }

    #endregion
}
