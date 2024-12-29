using System.ComponentModel;
using System.Reflection;

using Moq;
using Moq.Protected;

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
    public Task GenerateClassViewModel_ReturnsExpectedCode()
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

    // [Fact]
    // public Task GenerateClassViewModel_IncludesReferences()
    // {
    //     var generator = new TypeScriptViewModelGenerator();
    //     var typeMock = XmlDocIdTests.GetTypeMock( "Rock.ViewModels", "TestBag" );
    //     var otherTypeMock = XmlDocIdTests.GetTypeMock( "Rock.ViewModels", "OtherBag" );

    //     otherTypeMock.Protected().Setup<bool>( "IsPrimitiveImpl" ).Returns( false );
    //     otherTypeMock.Protected().Setup<bool>( "IsArrayImpl" ).Returns( false );
    //     otherTypeMock.Setup( m => m.IsEnum ).Returns( false );

    //     typeMock.Setup( m => m.GetProperties( It.IsAny<BindingFlags>() ) ).Returns( () => [
    //         GetPropertyMock( typeMock, "Bag", otherTypeMock.Object ).Object
    //     ] );

    //     var code = generator.GenerateClassViewModel( typeMock.Object );

    //     return Verify( code, Settings );
    // }

    // [Fact]
    // public Task GenerateClassViewModel_ExcludesNamedSelfImport()
    // {
    //     var generator = new TypeScriptViewModelGenerator();
    //     var typeMock = XmlDocIdTests.GetTypeMock( "Rock.ViewModels", "TestBag" );

    //     typeMock.Protected().Setup<bool>( "IsPrimitiveImpl" ).Returns( false );
    //     typeMock.Protected().Setup<bool>( "IsArrayImpl" ).Returns( false );
    //     typeMock.Setup( m => m.IsEnum ).Returns( false );

    //     typeMock.Setup( m => m.GetProperties( It.IsAny<BindingFlags>() ) ).Returns( () => [
    //         GetPropertyMock( typeMock, "Bag", typeMock.Object ).Object
    //     ] );

    //     var code = generator.GenerateClassViewModel( typeMock.Object );

    //     return Verify( code, Settings );
    // }

    #endregion

    #region GenerateClassesViewModel

    [Fact]
    public Task GenerateClassesViewModel_ReturnsExpectedCode()
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
    public Task GenerateEnumViewModel_ReturnsExpectedCode()
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

    #endregion

    #region GenerateDetailBlockTypeDefinitionFile

    #endregion

    #region GenerateListBlockTypeDefinitionFile

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

    #endregion

    #region Support Classes

    public class BaseViewModelBag
    {

    }

    #endregion
}
