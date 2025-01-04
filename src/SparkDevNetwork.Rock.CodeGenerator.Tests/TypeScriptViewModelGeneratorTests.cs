using System.ComponentModel;
using System.Reflection;
using System.Text;

using Moq;
using Moq.Protected;

using SparkDevNetwork.Rock.CodeGenerator.Documentation;
using SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation;

namespace SparkDevNetwork.Rock.CodeGenerator.Tests;

public class TypeScriptViewModelGeneratorTests
{
    private static readonly VerifySettings Settings;

    static TypeScriptViewModelGeneratorTests()
    {
        Settings = new VerifySettings();
        Settings.UseDirectory( "Snapshots" );
    }

    #region GenerateClassViewModel

    [Fact]
    public Task GenerateClassViewModel_GeneratesCode()
    {
        var generator = new TypeScriptViewModelGenerator();
        var typeMock = XmlDocIdTests.GetTypeMock( "Rock.ViewModels", "TestBag" );

        typeMock.Setup( m => m.GetProperties( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetPropertyMock<int>( typeMock, "Id" ).Object,
            GetPropertyMock<string>( typeMock, "Name" ).Object
        ] );

        var code = generator.GenerateClassViewModel( typeMock.Object );

        return Verify( code, Settings );
    }

    [Fact]
    public Task GenerateClassViewModel_IncludesGenericTypeArguments()
    {
        var generator = new TypeScriptViewModelGenerator();
        var typeMock = XmlDocIdTests.GetTypeMock( "Rock.ViewModels.Data", "TestBag" );
        var genericTypeMock = XmlDocIdTests.GetTypeMock( "Rock.ViewModels.Data", "TestBox" );

        genericTypeMock.Setup( m => m.IsGenericType ).Returns( true );
        genericTypeMock.Setup( m => m.GetGenericArguments() ).Returns( [typeMock.Object] );
        genericTypeMock.Setup( m => m.GetProperties( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetPropertyMock<int>( typeMock, "Id" ).Object
        ] );

        var code = generator.GenerateClassViewModel( genericTypeMock.Object );

        return Verify( code, Settings );
    }

    [Fact]
    public Task GenerateClassViewModel_IncludesTypeDocumentation()
    {
        var generator = new TypeScriptViewModelGenerator();
        var typeMock = XmlDocIdTests.GetTypeMock( "Rock.ViewModels", "TestBag" );
        var documentationProviderMock = new Mock<IDocumentationProvider>( MockBehavior.Strict );
        var summaryMock = new Mock<IComment>( MockBehavior.Strict );

        typeMock.Setup( m => m.GetProperties( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetPropertyMock<int>( typeMock, "Id" ).Object
        ] );

        summaryMock.Setup( m => m.PlainText ).Returns( "This is a test." );

        documentationProviderMock.Setup( m => m.GetTypeComments( typeMock.Object ) )
            .Returns( new XmlCommentSet( summaryMock.Object, null, null ) );
        documentationProviderMock.Setup( m => m.GetMemberComments( It.IsAny<MemberInfo>() ) )
            .Returns( ( ICommentSet ) null! );

        generator.DocumentationProvider = documentationProviderMock.Object;

        var code = generator.GenerateClassViewModel( typeMock.Object );

        return Verify( code, Settings );
    }

    #endregion

    #region GenerateClassesViewModel

    [Fact]
    public Task GenerateClassesViewModel_GeneratesCode()
    {
        var generator = new TypeScriptViewModelGenerator();
        var testBagMock = XmlDocIdTests.GetTypeMock( "Rock.ViewModels", "TestBag" );
        var otherBagMock = XmlDocIdTests.GetTypeMock( "Rock.ViewModels", "OtherBag" );

        testBagMock.Setup( m => m.GetProperties( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetPropertyMock<int>( testBagMock, "Id" ).Object
        ] );

        otherBagMock.Setup( m => m.GetProperties( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetPropertyMock<string>( otherBagMock, "Name" ).Object
        ] );

        var types = new List<Type>() { testBagMock.Object, otherBagMock.Object };

        var code = generator.GenerateClassesViewModel( types );

        return Verify( code, Settings );
    }

    #endregion

    #region GenerateEnumViewModel

    [Fact]
    public Task GenerateEnumViewModel_GeneratesCode()
    {
        var generator = new TypeScriptViewModelGenerator();
        var typeMock = GetEnumMock( "Rock.Enums", "TestSet" );

        typeMock.Setup( m => m.GetFields( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetEnumFieldMock( typeMock, "No", 0 ).Object,
            GetEnumFieldMock( typeMock, "Yes", 1 ).Object
        ] );

        var code = generator.GenerateEnumViewModel( typeMock.Object );

        return Verify( code, Settings );
    }

    [Fact]
    public Task GenerateEnumViewModel_MarksObsoleteFieldAsDeprecated()
    {
        var generator = new TypeScriptViewModelGenerator();
        var typeMock = GetEnumMock( "Rock.Enums", "TestSet" );

        typeMock.Setup( m => m.GetFields( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetEnumFieldMock( typeMock, "No", 0 ).Object,
            GetEnumFieldMock( typeMock, "Yes", 1, obsolete: true ).Object
        ] );

        var code = generator.GenerateEnumViewModel( typeMock.Object );

        return Verify( code, Settings );
    }

    [Fact]
    public Task GenerateEnumViewModel_IncludesObsoleteFieldReason()
    {
        var generator = new TypeScriptViewModelGenerator();
        var typeMock = GetEnumMock( "Rock.Enums", "TestSet" );

        typeMock.Setup( m => m.GetFields( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetEnumFieldMock( typeMock, "No", 0 ).Object,
            GetEnumFieldMock( typeMock, "Yes", 1, obsolete: true, obsoleteMessage: "Do not use this" ).Object
        ] );

        var code = generator.GenerateEnumViewModel( typeMock.Object );

        return Verify( code, Settings );
    }

    [Fact]
    public Task GenerateEnumViewModel_MarksObsoleteEnumAsDeprecated()
    {
        var generator = new TypeScriptViewModelGenerator();
        var typeMock = GetEnumMock( "Rock.Enums", "TestSet" );

        typeMock.Setup( m => m.GetCustomAttributesData() )
            .Returns( [GetObsoleteAttributeData().Object] );

        typeMock.Setup( m => m.GetFields( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetEnumFieldMock( typeMock, "No", 0 ).Object,
            GetEnumFieldMock( typeMock, "Yes", 1 ).Object
        ] );

        var code = generator.GenerateEnumViewModel( typeMock.Object );

        return Verify( code, Settings );
    }

    [Fact]
    public Task GenerateEnumViewModel_IncludesObsoleteEnumReason()
    {
        var generator = new TypeScriptViewModelGenerator();
        var typeMock = GetEnumMock( "Rock.Enums", "TestSet" );

        typeMock.Setup( m => m.GetCustomAttributesData() )
            .Returns( [GetObsoleteAttributeData( "Do not use this" ).Object] );

        typeMock.Setup( m => m.GetFields( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetEnumFieldMock( typeMock, "No", 0 ).Object,
            GetEnumFieldMock( typeMock, "Yes", 1 ).Object
        ] );

        var code = generator.GenerateEnumViewModel( typeMock.Object );

        return Verify( code, Settings );
    }

    [Fact]
    public Task GenerateEnumViewModel_ExcludesReplacedObsoleteFieldInDescription()
    {
        var generator = new TypeScriptViewModelGenerator();
        var typeMock = GetEnumMock( "Rock.Enums", "TestSet" );

        typeMock.Setup( m => m.GetFields( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetEnumFieldMock( typeMock, "No", 0 ).Object,
            GetEnumFieldMock( typeMock, "Yes", 1, obsolete: true ).Object,
            GetEnumFieldMock( typeMock, "True", 1, obsolete: false ).Object
        ] );

        var code = generator.GenerateEnumViewModel( typeMock.Object );

        return Verify( code, Settings );
    }

    [Fact]
    public Task GenerateEnumViewModel_EncodesFlagsValuesAsHex()
    {
        var generator = new TypeScriptViewModelGenerator();
        var typeMock = GetEnumMock( "Rock.Enums", "TestSet" );
        var flagsAttributeDataMock = new Mock<CustomAttributeData>( MockBehavior.Strict );

        flagsAttributeDataMock.Setup( m => m.AttributeType ).Returns( typeof( FlagsAttribute ) );
        flagsAttributeDataMock.Setup( m => m.ConstructorArguments ).Returns( [] );

        typeMock.Setup( m => m.GetCustomAttributesData() ).Returns( [flagsAttributeDataMock.Object] );

        typeMock.Setup( m => m.GetFields( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetEnumFieldMock( typeMock, "No", 0 ).Object,
            GetEnumFieldMock( typeMock, "Yes", 1 ).Object
        ] );

        var code = generator.GenerateEnumViewModel( typeMock.Object );

        return Verify( code, Settings );
    }

    [Fact]
    public Task GenerateEnumViewModel_EncodesNegativeValuesInBrackets()
    {
        var generator = new TypeScriptViewModelGenerator();
        var typeMock = GetEnumMock( "Rock.Enums", "TestSet" );

        typeMock.Setup( m => m.GetFields( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetEnumFieldMock( typeMock, "No", 0 ).Object,
            GetEnumFieldMock( typeMock, "Unknown", -1 ).Object
        ] );

        var code = generator.GenerateEnumViewModel( typeMock.Object );

        return Verify( code, Settings );
    }

    [Fact]
    public Task GenerateEnumViewModel_UsesFieldDescription()
    {
        var generator = new TypeScriptViewModelGenerator();
        var typeMock = GetEnumMock( "Rock.Enums", "TestSet" );

        typeMock.Setup( m => m.GetFields( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetEnumFieldMock( typeMock, "No", 0 ).Object,
            GetEnumFieldMock( typeMock, "Yes", 1, description: "Correct" ).Object
        ] );

        var code = generator.GenerateEnumViewModel( typeMock.Object );

        return Verify( code, Settings );
    }

    #endregion

    #region GenerateEnumsViewModel

    [Fact]
    public Task GenerateEnumsViewModel_IncludesAllTypes()
    {
        var generator = new TypeScriptViewModelGenerator();
        var typeAMock = GetEnumMock( "Rock.Enums", "TestSetA" );
        var typeBMock = GetEnumMock( "Rock.Enums", "TestSetB" );

        typeAMock.Setup( m => m.GetFields( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetEnumFieldMock( typeAMock, "No", 0 ).Object,
            GetEnumFieldMock( typeAMock, "Yes", 1 ).Object
        ] );

        typeBMock.Setup( m => m.GetFields( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetEnumFieldMock( typeAMock, "Maybe", 0 ).Object,
            GetEnumFieldMock( typeAMock, "Definitely", 1 ).Object
        ] );

        var code = generator.GenerateEnumsViewModel( [typeAMock.Object, typeBMock.Object] );

        return Verify( code, Settings );
    }

    #endregion

    #region GenerateSystemGuidForType

    [Fact]
    public Task GenerateSystemGuidForType_GeneratesCode()
    {
        var generator = new TypeScriptViewModelGenerator();
        var typeMock = XmlDocIdTests.GetTypeMock( "Rock.SystemGuid", "Test" );

        typeMock.Setup( m => m.GetFields( It.IsAny<BindingFlags>() ) ).Returns( () => [
            GetFieldMock( typeMock, "CAPS_NAME", "b85c07e2-1bdb-4868-96aa-af2ef58b5da3" ).Object,
            GetFieldMock( typeMock, "ITEM", "c650c429-6b4d-4edb-b7ce-340e1cc8fd44" ).Object
        ] );

        var code = generator.GenerateSystemGuidForType( typeMock.Object );

        return Verify( code, Settings );
    }

    #endregion

    #region GenerateDetailBlockTypeDefinitionFile

    [Fact]
    public Task GenerateDetailBlockTypeDefinitionFile_GeneratesCode()
    {
        var generator = new TypeScriptViewModelGenerator();
        var navigationUrls = new Dictionary<string, string>
        {
            ["ParentPage"] = "/abc",
            ["DetailPage"] = "/abc/{id}"
        };

        var code = generator.GenerateDetailBlockTypeDefinitionFile( navigationUrls );

        return Verify( code, Settings );
    }

    #endregion

    #region GenerateListBlockTypeDefinitionFile

    [Fact]
    public Task GenerateListBlockTypeDefinitionFile_GeneratesCode()
    {
        var generator = new TypeScriptViewModelGenerator();
        var navigationUrls = new Dictionary<string, string>
        {
            ["ParentPage"] = "/abc",
            ["DetailPage"] = "/abc/{id}"
        };

        var code = generator.GenerateListBlockTypeDefinitionFile( navigationUrls );

        return Verify( code, Settings );
    }

    #endregion

    #region AppendCommentBlock

    [Fact]
    public void AppendCommentBlock_EmptyCommentDoesNotEmit()
    {
        var sb = new StringBuilder();

        TypeScriptViewModelGenerator.AppendCommentBlock( sb, null, 0 );

        Assert.Empty( sb.ToString() );
    }

    [Fact]
    public Task AppendCommentBlock_ReplacesInlineCodeWithBackticks()
    {
        var sb = new StringBuilder();
        var comment = "This is <c>null</c>.";

        TypeScriptViewModelGenerator.AppendCommentBlock( sb, comment, 0 );

        return Verify( sb.ToString(), Settings );
    }

    [Fact]
    public Task AppendCommentBlock_ConvertsNewlinesToBlocks()
    {
        var sb = new StringBuilder();
        var comment = "This is\r\na block.";

        TypeScriptViewModelGenerator.AppendCommentBlock( sb, comment, 0 );

        return Verify( sb.ToString(), Settings );
    }

    [Fact]
    public Task AppendCommentBlock_ExtractsSeeCrefContent()
    {
        var sb = new StringBuilder();
        var comment = "This is an <see cref=\"T:Ignored\">item</see>.";

        TypeScriptViewModelGenerator.AppendCommentBlock( sb, comment, 0 );

        return Verify( sb.ToString(), Settings );
    }

    [Fact]
    public Task AppendCommentBlock_ExtractsRawSeeCrefTagThatsTooShort()
    {
        var sb = new StringBuilder();
        var comment = "This is an <see cref=\"T\" />.";

        TypeScriptViewModelGenerator.AppendCommentBlock( sb, comment, 0 );

        return Verify( sb.ToString(), Settings );
    }

    [Fact]
    public Task AppendCommentBlock_ExtractsRawSeeCrefTagMissingColon()
    {
        var sb = new StringBuilder();
        var comment = "This is an <see cref=\"Test\" />.";

        TypeScriptViewModelGenerator.AppendCommentBlock( sb, comment, 0 );

        return Verify( sb.ToString(), Settings );
    }

    [Fact]
    public Task AppendCommentBlock_ExtractsRawSeeCrefTagMissingPeriod()
    {
        var sb = new StringBuilder();
        var comment = "This is an <see cref=\"T:Root\" />.";

        TypeScriptViewModelGenerator.AppendCommentBlock( sb, comment, 0 );

        return Verify( sb.ToString(), Settings );
    }

    [Fact]
    public Task AppendCommentBlock_ConvertSeeCrefForEnum()
    {
        var sb = new StringBuilder();
        var comment = "This is an <see cref=\"F:Namespace.Enum.Value\" />.";

        TypeScriptViewModelGenerator.AppendCommentBlock( sb, comment, 0 );

        return Verify( sb.ToString(), Settings );
    }

    [Fact]
    public Task AppendCommentBlock_ConvertSeeCrefForProperty()
    {
        var sb = new StringBuilder();
        var comment = "This is an <see cref=\"P:Namespace.TestBag.Value\" />.";

        TypeScriptViewModelGenerator.AppendCommentBlock( sb, comment, 0 );

        return Verify( sb.ToString(), Settings );
    }

    #endregion

    #region GetTypeScriptTypeDefinition

    [Theory]
    [InlineData( typeof( bool ), "boolean" )]
    [InlineData( typeof( byte ), "number" )]
    [InlineData( typeof( sbyte ), "number" )]
    [InlineData( typeof( short ), "number" )]
    [InlineData( typeof( ushort ), "number" )]
    [InlineData( typeof( int ), "number" )]
    [InlineData( typeof( uint ), "number" )]
    [InlineData( typeof( long ), "number" )]
    [InlineData( typeof( ulong ), "number" )]
    [InlineData( typeof( decimal ), "number" )]
    [InlineData( typeof( float ), "number" )]
    [InlineData( typeof( double ), "number" )]
    [InlineData( typeof( string ), "string" )]
    [InlineData( typeof( DateTime ), "string" )]
    [InlineData( typeof( DateTimeOffset ), "string" )]
    public void GetTypeScriptTypeDefinition_FindsSimpleTypes( Type type, string expectedType )
    {
        var generator = new DefinitionTestClass();

        var definition = generator.CallGetTypeScriptTypeDefinition( type, true );

        Assert.Equal( expectedType, definition.Definition );
        Assert.Empty( definition.Imports );
    }

    [Theory]
    [InlineData( typeof( bool? ), "boolean | null" )]
    [InlineData( typeof( byte? ), "number | null" )]
    [InlineData( typeof( sbyte? ), "number | null" )]
    [InlineData( typeof( short? ), "number | null" )]
    [InlineData( typeof( ushort? ), "number | null" )]
    [InlineData( typeof( int? ), "number | null" )]
    [InlineData( typeof( uint? ), "number | null" )]
    [InlineData( typeof( long? ), "number | null" )]
    [InlineData( typeof( ulong? ), "number | null" )]
    [InlineData( typeof( decimal? ), "number | null" )]
    [InlineData( typeof( float? ), "number | null" )]
    [InlineData( typeof( double? ), "number | null" )]
    [InlineData( typeof( string ), "string | null" )]
    [InlineData( typeof( DateTime ), "string | null" )]
    [InlineData( typeof( DateTime? ), "string | null" )]
    [InlineData( typeof( DateTimeOffset ), "string | null" )]
    [InlineData( typeof( DateTimeOffset? ), "string | null" )]
    public void GetTypeScriptTypeDefinition_FindsOptionalSimpleString( Type type, string expectedType )
    {
        var generator = new DefinitionTestClass();

        var definition = generator.CallGetTypeScriptTypeDefinition( type, false );

        Assert.Equal( expectedType, definition.Definition );
        Assert.Empty( definition.Imports );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_FindsEnumType()
    {
        var generator = new DefinitionTestClass();

        var definition = generator.CallGetTypeScriptTypeDefinition( typeof( BindingFlags ), false );

        Assert.Equal( "number", definition.Definition );
        Assert.Empty( definition.Imports );
    }

    [Theory]
    [InlineData( typeof( Guid ), false, "Guid | null" )]
    [InlineData( typeof( Guid ), true, "Guid" )]
    [InlineData( typeof( Guid? ), false, "Guid | null" )]
    public void GetTypeScriptTypeDefinition_FindGuid( Type type, bool isRequired, string expectedType )
    {
        var generator = new DefinitionTestClass();

        var definition = generator.CallGetTypeScriptTypeDefinition( type, isRequired );

        Assert.Equal( expectedType, definition.Definition );
        Assert.Single( definition.Imports );
        Assert.Equal( "@Obsidian/Types", definition.Imports.First().SourcePath );
        Assert.Equal( "Guid", definition.Imports.First().NamedImport );
    }
    [Fact]
    public void GetTypeScriptTypeDefinition_FindRequiredGenericParameter()
    {
        var generator = new DefinitionTestClass();
        var typeMock = new Mock<Type>( MockBehavior.Strict );

        typeMock.Setup( m => m.IsGenericType ).Returns( false );
        typeMock.Setup( m => m.IsGenericParameter ).Returns( true );
        typeMock.Setup( m => m.FullName ).Returns( ( string? ) null );
        typeMock.Setup( m => m.Name ).Returns( "T" );

        var definition = generator.CallGetTypeScriptTypeDefinition( typeMock.Object, true );

        Assert.Equal( "T", definition.Definition );
        Assert.Empty( definition.Imports );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_FindOptionalGenericParameter()
    {
        var generator = new DefinitionTestClass();
        var typeMock = new Mock<Type>( MockBehavior.Strict );

        typeMock.Setup( m => m.IsGenericType ).Returns( false );
        typeMock.Setup( m => m.IsGenericParameter ).Returns( true );
        typeMock.Setup( m => m.FullName ).Returns( ( string? ) null );
        typeMock.Setup( m => m.Name ).Returns( "T" );

        var definition = generator.CallGetTypeScriptTypeDefinition( typeMock.Object, false );

        Assert.Equal( "T | null", definition.Definition );
        Assert.Empty( definition.Imports );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_FindNullableGenericParameter()
    {
        var generator = new DefinitionTestClass();
        var typeMock = new Mock<Type>( MockBehavior.Strict );

        typeMock.Setup( m => m.IsGenericType ).Returns( false );
        typeMock.Setup( m => m.IsGenericParameter ).Returns( true );
        typeMock.Setup( m => m.FullName ).Returns( ( string? ) null );
        typeMock.Setup( m => m.Name ).Returns( "T" );
        typeMock.Setup( m => m.IsSignatureType ).Returns( false );

        var nullableType = typeof( Nullable<> ).MakeGenericType( typeMock.Object );

        var definition = generator.CallGetTypeScriptTypeDefinition( nullableType, true );

        Assert.Equal( "T | null", definition.Definition );
        Assert.Empty( definition.Imports );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_FindRequiredArrayTypes()
    {
        var generator = new DefinitionTestClass();

        var definition = generator.CallGetTypeScriptTypeDefinition( typeof( int[] ), true );

        Assert.Equal( "number[]", definition.Definition );
        Assert.Empty( definition.Imports );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_FindOptionalArrayTypes()
    {
        var generator = new DefinitionTestClass();

        var definition = generator.CallGetTypeScriptTypeDefinition( typeof( int[] ), false );

        Assert.Equal( "number[] | null", definition.Definition );
        Assert.Empty( definition.Imports );
    }

    [Theory]
    [InlineData( typeof( Dictionary<string, int> ), true, "Record<string, number>" )]
    [InlineData( typeof( Dictionary<string, int> ), false, "Record<string, number> | null" )]
    public void GetTypeScriptTypeDefinition_FindDictionaryTypes( Type type, bool isRequired, string expectedType )
    {
        var generator = new DefinitionTestClass();

        var definition = generator.CallGetTypeScriptTypeDefinition( type, isRequired );

        Assert.Equal( expectedType, definition.Definition );
        Assert.Empty( definition.Imports );
    }

    [Theory]
    [InlineData( typeof( List<string> ), true, "string[]" )]
    [InlineData( typeof( List<string> ), false, "string[] | null" )]
    public void GetTypeScriptTypeDefinition_FindCollectionTypes( Type type, bool isRequired, string expectedType )
    {
        var generator = new DefinitionTestClass();

        var definition = generator.CallGetTypeScriptTypeDefinition( type, isRequired );

        Assert.Equal( expectedType, definition.Definition );
        Assert.Empty( definition.Imports );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_FindRequiredValidPropertiesBox()
    {
        var generator = new DefinitionTestClass();
        var typeDefinitionMock = new Mock<Type>( MockBehavior.Strict );
        var typeMock = new Mock<Type>( MockBehavior.Strict );

        typeDefinitionMock.Setup( m => m.IsGenericType ).Returns( false );
        typeDefinitionMock.Setup( m => m.IsGenericParameter ).Returns( false );
        typeDefinitionMock.Setup( m => m.FullName ).Returns( "Rock.ViewModels.Utility.ValidPropertiesBox`1" );

        typeMock.Protected().Setup<bool>( "IsArrayImpl" ).Returns( false );
        typeMock.Setup( m => m.IsGenericType ).Returns( true );
        typeMock.Setup( m => m.IsGenericParameter ).Returns( false );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.ViewModels.Utility.ValidPropertiesBox<int>" );
        typeMock.Setup( m => m.GetGenericArguments() ).Returns( [typeof( int )] );
        typeMock.Setup( m => m.GetGenericTypeDefinition() ).Returns( typeDefinitionMock.Object );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [] );

        var definition = generator.CallGetTypeScriptTypeDefinition( typeMock.Object, true );

        Assert.Equal( "ValidPropertiesBox<number>", definition.Definition );
        Assert.Single( definition.Imports );
        Assert.Equal( "@Obsidian/ViewModels/Utility/validPropertiesBox", definition.Imports.First().SourcePath );
        Assert.Equal( "ValidPropertiesBox", definition.Imports.First().NamedImport );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_FindOptionalValidPropertiesBox()
    {
        var generator = new DefinitionTestClass();
        var typeDefinitionMock = new Mock<Type>( MockBehavior.Strict );
        var typeMock = new Mock<Type>( MockBehavior.Strict );

        typeDefinitionMock.Setup( m => m.IsGenericType ).Returns( false );
        typeDefinitionMock.Setup( m => m.IsGenericParameter ).Returns( false );
        typeDefinitionMock.Setup( m => m.FullName ).Returns( "Rock.ViewModels.Utility.ValidPropertiesBox`1" );

        typeMock.Protected().Setup<bool>( "IsArrayImpl" ).Returns( false );
        typeMock.Setup( m => m.IsGenericType ).Returns( true );
        typeMock.Setup( m => m.IsGenericParameter ).Returns( false );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.ViewModels.Utility.ValidPropertiesBox<int>" );
        typeMock.Setup( m => m.GetGenericArguments() ).Returns( [typeof( int )] );
        typeMock.Setup( m => m.GetGenericTypeDefinition() ).Returns( typeDefinitionMock.Object );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [] );

        var definition = generator.CallGetTypeScriptTypeDefinition( typeMock.Object, false );

        Assert.Equal( "ValidPropertiesBox<number> | null", definition.Definition );
        Assert.Single( definition.Imports );
        Assert.Equal( "@Obsidian/ViewModels/Utility/validPropertiesBox", definition.Imports.First().SourcePath );
        Assert.Equal( "ValidPropertiesBox", definition.Imports.First().NamedImport );
    }

    [Theory]
    [InlineData( "Rock.ViewModels.Data", "TestBag", true, "@Obsidian/ViewModels/Data/testBag" )]
    [InlineData( "Rock.ViewModels.Data", "TestBag", false, "@Obsidian/ViewModels/Data/testBag" )]
    [InlineData( "Rock.ViewModels.Data", "TestBox", true, "@Obsidian/ViewModels/Data/testBox" )]
    public void GetTypeScriptTypeDefinition_FindViewModelBag( string typeNamespace, string typeName, bool isRequired, string importPath )
    {
        var generator = new DefinitionTestClass();
        var typeMock = XmlDocIdTests.GetTypeMock( typeNamespace, typeName );

        typeMock.Protected().Setup<bool>( "IsArrayImpl" ).Returns( false );

        var definition = generator.CallGetTypeScriptTypeDefinition( typeMock.Object, isRequired );

        if ( isRequired )
        {
            Assert.Equal( typeName, definition.Definition );
        }
        else
        {
            Assert.Equal( $"{typeName} | null", definition.Definition );
        }

        Assert.Single( definition.Imports );
        Assert.Equal( typeName, definition.Imports.First().NamedImport );
        Assert.Equal( importPath, definition.Imports.First().SourcePath );
    }

    [Theory]
    [InlineData( "Rock.Enums.Data", "TestSet", true, "@Obsidian/Enums/Data/testSet" )]
    [InlineData( "Rock.Enums.Data", "TestSet", false, "@Obsidian/Enums/Data/testSet" )]
    public void GetTypeScriptTypeDefinition_FindRockEnums( string typeNamespace, string typeName, bool isNullable, string importPath )
    {
        var generator = new DefinitionTestClass();
        var typeMock = GetEnumMock( typeNamespace, typeName );

        typeMock.Setup( m => m.IsSignatureType ).Returns( false );

        Type type = typeMock.Object;

        if ( isNullable )
        {
            type = typeof( Nullable<> ).MakeGenericType( type );
        }

        var definition = generator.CallGetTypeScriptTypeDefinition( type, true );

        if ( !isNullable )
        {
            Assert.Equal( typeName, definition.Definition );
        }
        else
        {
            Assert.Equal( $"{typeName} | null", definition.Definition );
        }

        Assert.Single( definition.Imports );
        Assert.Equal( typeName, definition.Imports.First().NamedImport );
        Assert.Equal( importPath, definition.Imports.First().SourcePath );
    }

    [Theory]
    [InlineData( "Data", "TestSet", true, "@Obsidian/Enums/Data/testSet" )]
    [InlineData( "Data", "TestSet", false, "@Obsidian/Enums/Data/testSet" )]
    public void GetTypeScriptTypeDefinition_FindRockDomainEnums( string domain, string typeName, bool isNullable, string importPath )
    {
        var generator = new DefinitionTestClass();
        var typeMock = GetEnumMock( "TestLib", typeName );

        typeMock.Setup( m => m.IsSignatureType ).Returns( false );
        typeMock.Setup( m => m.GetCustomAttributesData() )
            .Returns( [GetEnumDomainAttributeData( domain ).Object] );

        Type type = typeMock.Object;

        if ( isNullable )
        {
            type = typeof( Nullable<> ).MakeGenericType( type );
        }

        var definition = generator.CallGetTypeScriptTypeDefinition( type, true );

        if ( !isNullable )
        {
            Assert.Equal( typeName, definition.Definition );
        }
        else
        {
            Assert.Equal( $"{typeName} | null", definition.Definition );
        }

        Assert.Single( definition.Imports );
        Assert.Equal( typeName, definition.Imports.First().NamedImport );
        Assert.Equal( importPath, definition.Imports.First().SourcePath );
    }

    [Fact]
    public void GetTypeScriptTypeDefinition_EnumWithNullNamespaceIsNumber()
    {
        var generator = new DefinitionTestClass();
        var typeMock = GetEnumMock( null!, "InvalidState" );

        typeMock.Setup( m => m.IsSignatureType ).Returns( false );

        var definition = generator.CallGetTypeScriptTypeDefinition( typeMock.Object, true );

        Assert.Equal( "number", definition.Definition );
        Assert.Empty( definition.Imports );
    }

    #endregion

    #region Support Methods

    internal static Mock<PropertyInfo> GetPropertyMock<T>( Mock<Type> mockType, string propertyName )
    {
        return GetPropertyMock( mockType, propertyName, typeof( T ) );
    }

    internal static Mock<PropertyInfo> GetPropertyMock( Mock<Type> mockType, string propertyName, Type type )
    {
        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );

        propertyMock.Setup( m => m.Name ).Returns( propertyName );
        propertyMock.Setup( m => m.PropertyType ).Returns( type );
        propertyMock.Setup( m => m.MemberType ).Returns( MemberTypes.Property );
        propertyMock.Setup( m => m.DeclaringType ).Returns( () => mockType.Object );
        propertyMock.Setup( m => m.ReflectedType ).Returns( () => mockType.Object );

        return propertyMock;
    }

    internal static Mock<Type> GetEnumMock( string @namespace, string typeName )
    {
        var typeMock = XmlDocIdTests.GetTypeMock( @namespace, typeName );

        typeMock.Protected().Setup<bool>( "IsPrimitiveImpl" ).Returns( false );
        typeMock.Protected().Setup<bool>( "IsArrayImpl" ).Returns( false );
        typeMock.Setup( m => m.IsEnum ).Returns( true );
        typeMock.Setup( m => m.GetCustomAttributesData() ).Returns( [] );

        return typeMock;
    }

    internal static Mock<FieldInfo> GetEnumFieldMock( Mock<Type> mockType, string fieldName, int fieldValue, bool obsolete = false, string? obsoleteMessage = null, string? description = null )
    {
        var fieldMock = GetFieldMock( mockType, fieldName, typeof( int ) );

        var attributes = new List<CustomAttributeData>();

        if ( obsolete )
        {
            attributes.Add( GetObsoleteAttributeData( obsoleteMessage ).Object );
        }

        if ( description != null )
        {
            attributes.Add( GetDescriptionAttributeData( description ).Object );
        }

        fieldMock.Setup( m => m.GetRawConstantValue() ).Returns( fieldValue );
        fieldMock.Setup( m => m.GetCustomAttributesData() ).Returns( () => attributes );

        return fieldMock;
    }

    internal static Mock<FieldInfo> GetFieldMock<T>( Mock<Type> mockType, string fieldName, T fieldValue )
    {
        var fieldMock = GetFieldMock( mockType, fieldName, typeof( T ) );

        fieldMock.Setup( m => m.GetValue( null ) ).Returns( fieldValue );
        fieldMock.Setup( m => m.GetCustomAttributesData() ).Returns( [] );

        return fieldMock;
    }

    internal static Mock<FieldInfo> GetFieldMock( Mock<Type> mockType, string fieldName, Type type )
    {
        var fieldMock = new Mock<FieldInfo>( MockBehavior.Strict );

        fieldMock.Setup( m => m.Name ).Returns( fieldName );
        fieldMock.Setup( m => m.FieldType ).Returns( type );
        fieldMock.Setup( m => m.MemberType ).Returns( MemberTypes.Field );
        fieldMock.Setup( m => m.DeclaringType ).Returns( () => mockType.Object );
        fieldMock.Setup( m => m.ReflectedType ).Returns( () => mockType.Object );
        //        propertyMock.Setup( m => m.GetRawConstantValue() )

        return fieldMock;
    }

    internal static Mock<CustomAttributeData> GetObsoleteAttributeData( string? obsoleteMessage = null )
    {
        var attributeDataMock = new Mock<CustomAttributeData>( MockBehavior.Strict );

        attributeDataMock.Setup( m => m.AttributeType ).Returns( typeof( ObsoleteAttribute ) );
        attributeDataMock.Setup( m => m.ConstructorArguments ).Returns( () =>
        {
            var args = new List<CustomAttributeTypedArgument>();

            if ( obsoleteMessage != null )
            {
                args.Add( new CustomAttributeTypedArgument( obsoleteMessage ) );
            }

            return args;
        } );

        return attributeDataMock;
    }

    internal static Mock<CustomAttributeData> GetDescriptionAttributeData( string text )
    {
        var attributeDataMock = new Mock<CustomAttributeData>( MockBehavior.Strict );

        attributeDataMock.Setup( m => m.AttributeType ).Returns( typeof( DescriptionAttribute ) );
        attributeDataMock.Setup( m => m.ConstructorArguments )
            .Returns( [new CustomAttributeTypedArgument( text )] );

        return attributeDataMock;
    }

    internal static Mock<CustomAttributeData> GetEnumDomainAttributeData( string domain )
    {
        var attributeDataMock = new Mock<CustomAttributeData>( MockBehavior.Strict );
        var enumDomainTypeMock = XmlDocIdTests.GetTypeMock( "Rock.Enums", "EnumDomainAttribute" );

        attributeDataMock.Setup( m => m.AttributeType ).Returns( enumDomainTypeMock.Object );
        attributeDataMock.Setup( m => m.ConstructorArguments )
            .Returns( [new CustomAttributeTypedArgument( domain )] );

        return attributeDataMock;
    }

    #endregion

    #region Support Classes

    private class DefinitionTestClass : TypeScriptViewModelGenerator
    {
        public TypeScriptTypeDefinition CallGetTypeScriptTypeDefinition( Type type, bool isRequired )
        {
            return base.GetTypeScriptTypeDefinition( type, isRequired );
        }
    }

    #endregion
}
