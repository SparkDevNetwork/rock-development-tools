using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Runtime.Serialization;

using Moq;
using Moq.Protected;

using SparkDevNetwork.Rock.CodeGenerator.ListBlock;

namespace SparkDevNetwork.Rock.CodeGenerator.Tests.ListBlock;

public class ListBlockGeneratorTests
{
    private static readonly VerifySettings Settings;

    static ListBlockGeneratorTests()
    {
        Settings = new VerifySettings();
        Settings.UseDirectory( "Snapshots" );
    }

    #region GetEntityProperties

    [Fact]
    public void GetEntityProperties_ThrowsWithNullType()
    {
        Assert.Throws<ArgumentNullException>( () => ListBlockGenerator.GetEntityProperties( null, true ) );
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

        var foundProperties = ListBlockGenerator.GetEntityProperties( typeMock.Object, true );

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

        var foundProperties = ListBlockGenerator.GetEntityProperties( typeMock.Object, true );

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

        var foundProperties = ListBlockGenerator.GetEntityProperties( entityTypeMock.Object, true );

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

        var foundProperties = ListBlockGenerator.GetEntityProperties( typeMock.Object, true );

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

        var foundProperties = ListBlockGenerator.GetEntityProperties( typeMock.Object, true );

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

        var foundProperties = ListBlockGenerator.GetEntityProperties( typeMock.Object, false );

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

        var foundProperties = ListBlockGenerator.GetEntityProperties( typeMock.Object, true );

        Assert.NotEmpty( foundProperties );
    }

    [Fact]
    public void GetEntityProperties_ExcludesEntityIdPropertyWithNavigationProperty()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        var entityTypeMock = new Mock<Type>( MockBehavior.Strict );

        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );
        entityTypeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );
        entityTypeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );

        var dataMemberMock = new Mock<CustomAttributeData>( MockBehavior.Strict );
        dataMemberMock.Setup( m => m.AttributeType ).Returns( typeof( DataMemberAttribute ) );

        var navigationPropertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        navigationPropertyMock.Setup( m => m.GetCustomAttributesData() ).Returns( [] );
        navigationPropertyMock.Setup( m => m.PropertyType ).Returns( entityTypeMock.Object );
        navigationPropertyMock.Setup( m => m.Name ).Returns( "DefinedValue" );

        var entityIdPropertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        entityIdPropertyMock.Setup( m => m.GetCustomAttributesData() ).Returns( [dataMemberMock.Object] );
        entityIdPropertyMock.Setup( m => m.PropertyType ).Returns( entityTypeMock.Object );
        entityIdPropertyMock.Setup( m => m.Name ).Returns( "DefinedValueId" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        entityTypeMock.Setup( m => m.GetProperties( It.IsAny<BindingFlags>() ) )
            .Returns( [navigationPropertyMock.Object, entityIdPropertyMock.Object] );

        var foundProperties = ListBlockGenerator.GetEntityProperties( entityTypeMock.Object, false );

        Assert.Single( foundProperties );
    }

    #endregion

    #region IsPropertyValidForColumn

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
    [InlineData( typeof( float ) )]
    [InlineData( typeof( float? ) )]
    [InlineData( typeof( double ) )]
    [InlineData( typeof( double? ) )]
    public void IsPropertyValidForColumn_ReturnsTrueForExpectedTypes( Type type )
    {
        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyMock.Setup( m => m.PropertyType ).Returns( type );

        var isSupported = ListBlockGenerator.IsPropertyValidForColumn( propertyMock.Object );

        Assert.True( isSupported );
    }

    [Fact]
    public void IsPropertyValidForColumn_ReturnsTrueForEntityType()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        var entityTypeMock = new Mock<Type>( MockBehavior.Strict );

        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );
        entityTypeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );
        entityTypeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );

        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyMock.Setup( m => m.PropertyType ).Returns( entityTypeMock.Object );

        var isSupported = ListBlockGenerator.IsPropertyValidForColumn( propertyMock.Object );

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
        entityTypeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );
        entityTypeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );
        entityTypeMock.Protected()
            .Setup<PropertyInfo?>( "GetPropertyImpl", [
                "Name",
                ItExpr.IsAny<BindingFlags>(),
                ItExpr.IsAny<Binder>(),
                ItExpr.IsAny<Type>(),
                ItExpr.IsAny<Type[]>(),
                ItExpr.IsAny<ParameterModifier[]>()
            ] )
            .Returns( new Mock<PropertyInfo>( MockBehavior.Strict ).Object );

        var navigationPropertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        navigationPropertyMock.Setup( m => m.GetCustomAttributesData() ).Returns( [] );
        navigationPropertyMock.Setup( m => m.PropertyType ).Returns( entityTypeMock.Object );
        navigationPropertyMock.Setup( m => m.Name ).Returns( "ButtonTypeValue" );

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

        var generator = new ListBlockGenerator( new FluidTemplateRenderer() );
        var options = new ListBlockOptions
        {
            BlockEntityGuid = new Guid( "3acec49f-2771-43aa-8ec9-3464a278c0ec" ),
            BlockNamespace = "Rock.Blocks.Core",
            BlockTypeGuid = new Guid( "5705f86c-10fd-4ad3-97b5-6094f965067d" ),
            Category = "Core",
            Columns = [
                new EntityColumn( amountPropertyMock.Object ),
                new EntityColumn( entityIdPropertyMock.Object ),
                new EntityColumn( navigationPropertyMock.Object ),
                new EntityColumn( datePropertyMock.Object )
            ],
            CSharpBlockRelativePath = "Rock.Blocks\\Core",
            EntityTypeGuid = new Guid( "8b37d8c9-3aa8-42a5-a723-98a26a1f065f" ),
            EntityTypeName = "MyObject",
            ExpectedRowCount = null,
            IsPlugin = false,
            ServiceTypeName = "MyObjectService",
            ShowDelete = true,
            ShowReorder = true,
            ShowSecurity = true,
            ToolTipSource = "Description",
            TypeScriptBagImportPath = "@Obsidian/ViewModels/Blocks/Core/MyObjectList",
            TypeScriptBlockRelativePath = "Rock.JavaScript.Obsidian.Blocks\\src\\Core",
            UseAttributeValues = true,
            UseEntitySecurity = true,
            UseIsSystem = true,
            ViewModelCSharpRelativePath = "Rock.ViewModels\\Blocks\\Core\\MyObjectList",
            ViewModelNamespace = "Rock.ViewModels.Blocks.Core.MyObjectList",
        };

        var files = generator.GenerateFiles( options );

        Assert.Equal( 4, files.Count );

        Assert.Equal( "MyObjectListOptionsBag.cs", files[0].FileName );
        Assert.Equal( "Rock.ViewModels\\Blocks\\Core\\MyObjectList\\MyObjectListOptionsBag.cs", files[0].SolutionRelativePath );
        Assert.Equal( GeneratedFileSaveState.NotProcessed, files[0].SaveState );

        Assert.Equal( "MyObjectList.cs", files[1].FileName );
        Assert.Equal( "Rock.Blocks\\Core\\MyObjectList.cs", files[1].SolutionRelativePath );
        Assert.Equal( GeneratedFileSaveState.NotProcessed, files[1].SaveState );

        Assert.Equal( "myObjectList.obs", files[2].FileName );
        Assert.Equal( "Rock.JavaScript.Obsidian.Blocks\\src\\Core\\myObjectList.obs", files[2].SolutionRelativePath );
        Assert.Equal( GeneratedFileSaveState.NotProcessed, files[2].SaveState );

        Assert.Equal( "types.partial.ts", files[3].FileName );
        Assert.Equal( "Rock.JavaScript.Obsidian.Blocks\\src\\Core\\MyObjectList\\types.partial.ts", files[3].SolutionRelativePath.Replace( '/', '\\' ) );
        Assert.Equal( GeneratedFileSaveState.NotProcessed, files[3].SaveState );

        return Task.WhenAll(
            Verify( files[0].Content, Settings )
                .UseMethodName( $"{nameof( GeneratedFilesProducesExpectedOutput )}_MyObjectListOptionsBag.cs" ),
            Verify( files[1].Content, Settings )
                .UseMethodName( $"{nameof( GeneratedFilesProducesExpectedOutput )}_MyObjectList.cs" ),
            Verify( files[2].Content, Settings )
                .UseMethodName( $"{nameof( GeneratedFilesProducesExpectedOutput )}_myObjectList.obs" ),
            Verify( files[3].Content, Settings )
                .UseMethodName( $"{nameof( GeneratedFilesProducesExpectedOutput )}_types.partial.ts" )
        );
    }

    #endregion
}
