using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Runtime.Serialization;

using Moq;
using Moq.Protected;

using SparkDevNetwork.Rock.CodeGenerator.DetailBlock;

namespace SparkDevNetwork.Rock.CodeGenerator.Tests.DetailBlock;

public class DetailBlockGeneratorTests
{
    private static readonly VerifySettings Settings;

    static DetailBlockGeneratorTests()
    {
        Settings = new VerifySettings();
        Settings.UseDirectory( "Snapshots" );
    }

    #region GetEntityProperties

    [Fact]
    public void GetEntityProperties_ThrowsWithNullType()
    {
        Assert.Throws<ArgumentNullException>( () => DetailBlockGenerator.GetEntityProperties( null, true ) );
    }

    [Fact]
    public void GetEntityProperties_ExcludesUntaggedProperty()
    {
        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyMock.Setup( m => m.GetCustomAttributesData() ).Returns( [] );
        propertyMock.Setup( m => m.PropertyType ).Returns( typeof( int ) );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.GetProperties( It.IsAny<BindingFlags>() ) )
            .Returns( [propertyMock.Object] );

        var foundProperties = DetailBlockGenerator.GetEntityProperties( typeMock.Object, true );

        Assert.Empty( foundProperties );
    }

    [Fact]
    public void GetEntityProperties_IncludesTaggedProperty()
    {
        var dataMemberMock = new Mock<CustomAttributeData>( MockBehavior.Strict );
        dataMemberMock.Setup( m => m.AttributeType ).Returns( typeof( DataMemberAttribute ) );

        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyMock.Setup( m => m.GetCustomAttributesData() ).Returns( [dataMemberMock.Object] );
        propertyMock.Setup( m => m.PropertyType ).Returns( typeof( string ) );
        propertyMock.Setup( m => m.Name ).Returns( "Value" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.GetProperties( It.IsAny<BindingFlags>() ) )
            .Returns( [propertyMock.Object] );

        var foundProperties = DetailBlockGenerator.GetEntityProperties( typeMock.Object, true );

        Assert.NotEmpty( foundProperties );
    }

    [Fact]
    public void GetEntityProperties_IncludesEntityProperty()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        var entityTypeMock = new Mock<Type>( MockBehavior.Strict );

        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );
        entityTypeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );
        entityTypeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );

        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyMock.Setup( m => m.GetCustomAttributesData() ).Returns( [] );
        propertyMock.Setup( m => m.PropertyType ).Returns( entityTypeMock.Object );
        propertyMock.Setup( m => m.Name ).Returns( "DefinedValue" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        entityTypeMock.Setup( m => m.GetProperties( It.IsAny<BindingFlags>() ) )
            .Returns( [propertyMock.Object] );

        var foundProperties = DetailBlockGenerator.GetEntityProperties( entityTypeMock.Object, true );

        Assert.NotEmpty( foundProperties );
    }

    [Fact]
    public void GetEntityProperties_ExcludesNotMappedProperty()
    {
        var dataMemberMock = new Mock<CustomAttributeData>( MockBehavior.Strict );
        dataMemberMock.Setup( m => m.AttributeType ).Returns( typeof( DataMemberAttribute ) );

        var notMappedMock = new Mock<CustomAttributeData>( MockBehavior.Strict );
        notMappedMock.Setup( m => m.AttributeType ).Returns( typeof( NotMappedAttribute ) );

        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyMock.Setup( m => m.GetCustomAttributesData() ).Returns( [dataMemberMock.Object, notMappedMock.Object] );
        propertyMock.Setup( m => m.PropertyType ).Returns( typeof( string ) );
        propertyMock.Setup( m => m.Name ).Returns( "Value" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.GetProperties( It.IsAny<BindingFlags>() ) )
            .Returns( [propertyMock.Object] );

        var foundProperties = DetailBlockGenerator.GetEntityProperties( typeMock.Object, true );

        Assert.Empty( foundProperties );
    }

    [Fact]
    public void GetEntityProperties_ExcludesSystemProperty()
    {
        var dataMemberMock = new Mock<CustomAttributeData>( MockBehavior.Strict );
        dataMemberMock.Setup( m => m.AttributeType ).Returns( typeof( DataMemberAttribute ) );

        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyMock.Setup( m => m.GetCustomAttributesData() ).Returns( [dataMemberMock.Object] );
        propertyMock.Setup( m => m.PropertyType ).Returns( typeof( int ) );
        propertyMock.Setup( m => m.Name ).Returns( "Id" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.GetProperties( It.IsAny<BindingFlags>() ) )
            .Returns( [propertyMock.Object] );

        var foundProperties = DetailBlockGenerator.GetEntityProperties( typeMock.Object, true );

        Assert.Empty( foundProperties );
    }

    [Fact]
    public void GetEntityProperties_ExcludesAdvancedPropertyWhenNotRequested()
    {
        var dataMemberMock = new Mock<CustomAttributeData>( MockBehavior.Strict );
        dataMemberMock.Setup( m => m.AttributeType ).Returns( typeof( DataMemberAttribute ) );

        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyMock.Setup( m => m.GetCustomAttributesData() ).Returns( [dataMemberMock.Object] );
        propertyMock.Setup( m => m.PropertyType ).Returns( typeof( int ) );
        propertyMock.Setup( m => m.Name ).Returns( "ForeignId" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.GetProperties( It.IsAny<BindingFlags>() ) )
            .Returns( [propertyMock.Object] );

        var foundProperties = DetailBlockGenerator.GetEntityProperties( typeMock.Object, false );

        Assert.Empty( foundProperties );
    }

    [Fact]
    public void GetEntityProperties_IncludesAdvancedPropertyWhenRequested()
    {
        var dataMemberMock = new Mock<CustomAttributeData>( MockBehavior.Strict );
        dataMemberMock.Setup( m => m.AttributeType ).Returns( typeof( DataMemberAttribute ) );

        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyMock.Setup( m => m.GetCustomAttributesData() ).Returns( [dataMemberMock.Object] );
        propertyMock.Setup( m => m.PropertyType ).Returns( typeof( int ) );
        propertyMock.Setup( m => m.Name ).Returns( "ForeignId" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.GetProperties( It.IsAny<BindingFlags>() ) )
            .Returns( [propertyMock.Object] );

        var foundProperties = DetailBlockGenerator.GetEntityProperties( typeMock.Object, true );

        Assert.NotEmpty( foundProperties );
    }

    #endregion

    #region IsPropertyValid

    [Theory]
    [InlineData( typeof( string ) )]
    [InlineData( typeof( Guid ) )]
    [InlineData( typeof( Guid? ) )]
    [InlineData( typeof( bool ) )]
    [InlineData( typeof( bool? ) )]
    [InlineData( typeof( int ) )]
    [InlineData( typeof( int? ) )]
    [InlineData( typeof( decimal ) )]
    [InlineData( typeof( decimal? ) )]
    [InlineData( typeof( double ) )]
    [InlineData( typeof( double? ) )]
    public void IsPropertyValid_ReturnsTrueForExpectedTypes( Type type )
    {
        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyMock.Setup( m => m.PropertyType ).Returns( type );

        var isSupported = DetailBlockGenerator.IsPropertyValid( propertyMock.Object );

        Assert.True( isSupported );
    }

    [Fact]
    public void IsPropertyValid_ReturnsTrueForEntityType()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        var entityTypeMock = new Mock<Type>( MockBehavior.Strict );

        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );
        entityTypeMock.Setup( m => m.IsGenericType ).Returns( false );
        entityTypeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );
        entityTypeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );

        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyMock.Setup( m => m.PropertyType ).Returns( entityTypeMock.Object );

        var isSupported = DetailBlockGenerator.IsPropertyValid( propertyMock.Object );

        Assert.True( isSupported );
    }

    #endregion

    #region GenerateFiles

    [Fact]
    public Task GeneratedFilesProducesExpectedOutput()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        var entityTypeMock = new Mock<Type>( MockBehavior.Strict );

        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );
        entityTypeMock.Setup( m => m.IsGenericType ).Returns( false );
        entityTypeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );
        entityTypeMock.Setup( m => m.Name ).Returns( "DefinedValue" );
        entityTypeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );
        entityTypeMock.Protected()
            .Setup<PropertyInfo?>( "GetPropertyImpl", [
                "ButtonTypeValueId",
                ItExpr.IsAny<BindingFlags>(),
                ItExpr.IsAny<Binder>(),
                ItExpr.IsAny<Type>(),
                ItExpr.IsAny<Type[]>(),
                ItExpr.IsAny<ParameterModifier[]>()
            ] )
            .Returns( ( PropertyInfo? ) null );

        var navigationPropertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        navigationPropertyMock.Setup( m => m.GetCustomAttributesData() ).Returns( [] );
        navigationPropertyMock.Setup( m => m.PropertyType ).Returns( entityTypeMock.Object );
        navigationPropertyMock.Setup( m => m.Name ).Returns( "ButtonTypeValue" );
        navigationPropertyMock.Setup( m => m.DeclaringType ).Returns( entityTypeMock.Object );

        var entityIdPropertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        entityIdPropertyMock.Setup( m => m.GetCustomAttributesData() ).Returns( [] );
        entityIdPropertyMock.Setup( m => m.PropertyType ).Returns( typeof( int ) );
        entityIdPropertyMock.Setup( m => m.Name ).Returns( "DefinedValueId" );

        var amountPropertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        amountPropertyMock.Setup( m => m.GetCustomAttributesData() ).Returns( [] );
        amountPropertyMock.Setup( m => m.PropertyType ).Returns( typeof( double ) );
        amountPropertyMock.Setup( m => m.Name ).Returns( "Amount" );

        var datePropertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        datePropertyMock.Setup( m => m.GetCustomAttributesData() ).Returns( [] );
        datePropertyMock.Setup( m => m.PropertyType ).Returns( typeof( DateTime? ) );
        datePropertyMock.Setup( m => m.Name ).Returns( "TransactionDateTime" );

        var generator = new DetailBlockGenerator( new FluidTemplateRenderer() );
        var options = new DetailBlockOptions
        {
            BlockEntityGuid = new Guid( "3acec49f-2771-43aa-8ec9-3464a278c0ec" ),
            BlockNamespace = "Rock.Blocks.Core",
            BlockTypeGuid = new Guid( "5705f86c-10fd-4ad3-97b5-6094f965067d" ),
            Category = "Core",
            Properties = [
                new EntityProperty( amountPropertyMock.Object ),
                new EntityProperty( entityIdPropertyMock.Object ),
                new EntityProperty( navigationPropertyMock.Object ),
                new EntityProperty( datePropertyMock.Object )
            ],
            CSharpBlockRelativePath = "Rock.Blocks\\Core",
            EntityTypeName = "MyObject",
            IsPlugin = false,
            ServiceTypeName = "MyObjectService",
            TypeScriptBagImportPath = "@Obsidian/ViewModels/Blocks/Core/MyObjectDetail",
            TypeScriptBlockRelativePath = "Rock.JavaScript.Obsidian.Blocks\\src\\Core",
            UseAttributeValues = true,
            UseEntitySecurity = true,
            ViewModelCSharpRelativePath = "Rock.ViewModels\\Blocks\\Core\\MyObjectDetail",
            ViewModelNamespace = "Rock.ViewModels.Blocks.Core.MyObjectDetail",
        };

        var files = generator.GenerateFiles( options );

        Assert.Equal( 7, files.Count );

        Assert.Equal( "MyObjectBag.cs", files[0].FileName );
        Assert.Equal( "Rock.ViewModels\\Blocks\\Core\\MyObjectDetail\\MyObjectBag.cs", files[0].SolutionRelativePath );
        Assert.Equal( GeneratedFileSaveState.NotProcessed, files[0].SaveState );

        Assert.Equal( "MyObjectDetailOptionsBag.cs", files[1].FileName );
        Assert.Equal( "Rock.ViewModels\\Blocks\\Core\\MyObjectDetail\\MyObjectDetailOptionsBag.cs", files[1].SolutionRelativePath );
        Assert.Equal( GeneratedFileSaveState.NotProcessed, files[1].SaveState );

        Assert.Equal( "MyObjectDetail.cs", files[2].FileName );
        Assert.Equal( "Rock.Blocks\\Core\\MyObjectDetail.cs", files[2].SolutionRelativePath );
        Assert.Equal( GeneratedFileSaveState.NotProcessed, files[2].SaveState );

        Assert.Equal( "myObjectDetail.obs", files[3].FileName );
        Assert.Equal( "Rock.JavaScript.Obsidian.Blocks\\src\\Core\\myObjectDetail.obs", files[3].SolutionRelativePath );
        Assert.Equal( GeneratedFileSaveState.NotProcessed, files[3].SaveState );

        Assert.Equal( "viewPanel.partial.obs", files[4].FileName );
        Assert.Equal( "Rock.JavaScript.Obsidian.Blocks\\src\\Core\\MyObjectDetail\\viewPanel.partial.obs", files[4].SolutionRelativePath );
        Assert.Equal( GeneratedFileSaveState.NotProcessed, files[4].SaveState );

        Assert.Equal( "editPanel.partial.obs", files[5].FileName );
        Assert.Equal( "Rock.JavaScript.Obsidian.Blocks\\src\\Core\\MyObjectDetail\\editPanel.partial.obs", files[5].SolutionRelativePath );
        Assert.Equal( GeneratedFileSaveState.NotProcessed, files[5].SaveState );

        Assert.Equal( "types.partial.ts", files[6].FileName );
        Assert.Equal( "Rock.JavaScript.Obsidian.Blocks\\src\\Core\\MyObjectDetail\\types.partial.ts", files[6].SolutionRelativePath );
        Assert.Equal( GeneratedFileSaveState.NotProcessed, files[6].SaveState );

        return Task.WhenAll(
            Verify( files[0].Content, Settings )
                .UseMethodName( $"{nameof( GeneratedFilesProducesExpectedOutput )}_{files[0].FileName}" ),
            Verify( files[1].Content, Settings )
                .UseMethodName( $"{nameof( GeneratedFilesProducesExpectedOutput )}_{files[1].FileName}" ),
            Verify( files[2].Content, Settings )
                .UseMethodName( $"{nameof( GeneratedFilesProducesExpectedOutput )}_{files[2].FileName}" ),
            Verify( files[3].Content, Settings )
                .UseMethodName( $"{nameof( GeneratedFilesProducesExpectedOutput )}_{files[3].FileName}" ),
            Verify( files[4].Content, Settings )
                .UseMethodName( $"{nameof( GeneratedFilesProducesExpectedOutput )}_{files[4].FileName}" ),
            Verify( files[5].Content, Settings )
                .UseMethodName( $"{nameof( GeneratedFilesProducesExpectedOutput )}_{files[5].FileName}" ),
            Verify( files[6].Content, Settings )
                .UseMethodName( $"{nameof( GeneratedFilesProducesExpectedOutput )}_{files[6].FileName}" )
        );
    }

    #endregion

    #region Support Methods

    /// <summary>
    /// Creates a mock template renderer that returns an empty string.
    /// </summary>
    /// <returns>A mocked template renderer.</returns>
    private static ITemplateRenderer MockRenderer()
    {
        var rendererMock = new Mock<ITemplateRenderer>( MockBehavior.Strict );

        rendererMock.Setup( m => m.Render( It.IsAny<string>(), It.IsAny<Dictionary<string, object>>() ) )
            .Returns( string.Empty );

        return rendererMock.Object;
    }

    #endregion
}
