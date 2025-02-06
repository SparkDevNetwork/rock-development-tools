using System.Reflection;

using Moq;

using SparkDevNetwork.Rock.CodeGenerator.Documentation;

namespace SparkDevNetwork.Rock.CodeGenerator.Tests;

public class CSharpViewModelGeneratorTests
{
    private static readonly VerifySettings Settings;

    static CSharpViewModelGeneratorTests()
    {
        Settings = new VerifySettings();
        Settings.UseDirectory( "Snapshots" );
    }

    [Fact]
    public Task GenerateOptionsBag_CreatesEmptyClass()
    {
        var generator = new CSharpViewModelGenerator();

        var code = generator.GenerateOptionsBag( "TestBag", "Rock.ViewModels.Test", "A test bag." );

        return Verify( code, Settings );
    }

    [Fact]
    public Task GenerateEntityBag_IncludesAllProperties()
    {
        // Create a fake entity to be used as a property.
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var entityTypeMock = new Mock<Type>( MockBehavior.Strict );
        entityTypeMock.Setup( m => m.IsGenericType ).Returns( false );
        entityTypeMock.Setup( m => m.IsEnum ).Returns( false );
        entityTypeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );
        entityTypeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );

        // Create various properties with different comment types
        // as well as data types.
        var propertyInfoWithCommentMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyInfoWithCommentMock.Setup( m => m.PropertyType ).Returns( typeof( int ) );
        propertyInfoWithCommentMock.Setup( m => m.Name ).Returns( "ItemId" );

        var propertyInfoWithEmptyCommentMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyInfoWithEmptyCommentMock.Setup( m => m.PropertyType ).Returns( typeof( Guid ) );
        propertyInfoWithEmptyCommentMock.Setup( m => m.Name ).Returns( "ItemGuid" );

        var propertyInfoWithNoCommentMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyInfoWithNoCommentMock.Setup( m => m.PropertyType ).Returns( typeof( int? ) );
        propertyInfoWithNoCommentMock.Setup( m => m.Name ).Returns( "OtherId" );

        var propertyInfoWithNullSummaryCommentMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyInfoWithNullSummaryCommentMock.Setup( m => m.PropertyType ).Returns( typeof( List<Attribute> ) );
        propertyInfoWithNullSummaryCommentMock.Setup( m => m.Name ).Returns( "Items" );

        var propertyInfoWithEntityMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyInfoWithEntityMock.Setup( m => m.PropertyType ).Returns( entityTypeMock.Object );
        propertyInfoWithEntityMock.Setup( m => m.Name ).Returns( "DefinedValue" );

        // Construct a fake documentation provide to return differnet comment
        // values for different properties.
        var documentation = new Mock<IDocumentationProvider>( MockBehavior.Strict );
        documentation.Setup( m => m.GetMemberComments( It.IsAny<MemberInfo>() ) ).Returns<MemberInfo>( mi =>
        {
            if ( mi.Name == "ItemId" )
            {
                var summary = new Mock<IComment>( MockBehavior.Strict );
                summary.Setup( m => m.PlainText ).Returns( "A comment for item." );
                return new XmlCommentSet( summary.Object, null, null );
            }
            else if ( mi.Name == "ItemGuid" )
            {
                var summary = new Mock<IComment>( MockBehavior.Strict );
                summary.Setup( m => m.PlainText ).Returns( string.Empty );
                return new XmlCommentSet( summary.Object, null, null );
            }
            else if ( mi.Name == "Items" )
            {
                var summary = new Mock<IComment>( MockBehavior.Strict );
                summary.Setup( m => m.PlainText ).Returns<IComment>( null! );
                return new XmlCommentSet( summary.Object, null, null );
            }
            else
            {
                return null!;
            }
        } );

        var generator = new CSharpViewModelGenerator
        {
            DocumentationProvider = documentation.Object
        };

        var properties = new List<EntityProperty>
        {
            new( propertyInfoWithCommentMock.Object ),
            new( propertyInfoWithEmptyCommentMock.Object ),
            new( propertyInfoWithNoCommentMock.Object ),
            new( propertyInfoWithNullSummaryCommentMock.Object ),
            new( propertyInfoWithEntityMock.Object )
        };

        var code = generator.GenerateEntityBag( "DefinedValue", "Rock.ViewModels.Test", properties, "A test entity bag." );

        return Verify( code, Settings );
    }
}
