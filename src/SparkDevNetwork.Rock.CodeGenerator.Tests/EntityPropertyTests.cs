using System.Reflection;
using System.Text;

using Moq;
using Moq.Protected;

namespace SparkDevNetwork.Rock.CodeGenerator.Tests;

public class EntityPropertyTests
{
    #region IsAssignmentType

    [Theory]
    [InlineData( typeof( bool ) )]
    [InlineData( typeof( bool? ) )]
    [InlineData( typeof( int ) )]
    [InlineData( typeof( int? ) )]
    [InlineData( typeof( long ) )]
    [InlineData( typeof( long? ) )]
    [InlineData( typeof( decimal ) )]
    [InlineData( typeof( decimal? ) )]
    [InlineData( typeof( double ) )]
    [InlineData( typeof( double? ) )]
    [InlineData( typeof( string ) )]
    [InlineData( typeof( Guid ) )]
    [InlineData( typeof( Guid? ) )]
    [InlineData( typeof( DateTime ) )]
    [InlineData( typeof( DateTime? ) )]
    [InlineData( typeof( DateTimeOffset ) )]
    [InlineData( typeof( DateTimeOffset? ) )]
    public void IsAssignmentType_WithAssignmentTypes_ReturnsTrue( Type type )
    {
        Assert.True( EntityProperty.IsAssignmentType( type ), $"Expected type '{type.FullName}' was not considered assignment type." );
    }

    [Theory]
    [InlineData( typeof( EntityProperty ) )]
    [InlineData( typeof( char ) )]
    public void IsAssignmentType_WithNonAssignmentTypes_ReturnsFalse( Type type )
    {
        Assert.False( EntityProperty.IsAssignmentType( type ), $"Unxpected type '{type.FullName}' was considered assignment type." );
    }

    #endregion

    #region IsSupportedPropertyType

    [Fact]
    public void IsSupportedPropertyType_WithSupportedCollection_ReturnsTrue()
    {
        Assert.True( EntityProperty.IsSupportedPropertyType( typeof( List<string> ) ) );
    }

    [Fact]
    public void IsSupportedPropertyType_WithUnsupportedCollection_ReturnsFalse()
    {
        Assert.False( EntityProperty.IsSupportedPropertyType( typeof( List<EntityProperty> ) ) );
    }

    [Fact]
    public void IsSupportedPropertyType_WithNonCollectionGeneric_ReturnsFalse()
    {
        Assert.False( EntityProperty.IsSupportedPropertyType( typeof( NonCollectionGeneric<string> ) ) );
    }

    [Fact]
    public void IsSupportedPropertyType_WithEntityType_ReturnsTrue()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.IsGenericType ).Returns( false );
        typeMock.Setup( m => m.IsEnum ).Returns( false );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );

        Assert.True( EntityProperty.IsSupportedPropertyType( typeMock.Object ) );
    }

    [Fact]
    public void IsSupportedPropertyType_WithAssignmentType_ReturnsTrue()
    {
        Assert.True( EntityProperty.IsSupportedPropertyType( typeof( int ) ) );
    }

    [Fact]
    public void IsSupportedPropertyType_WithRockEnumType_ReturnsTrue()
    {
        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.IsGenericType ).Returns( false );
        typeMock.Setup( m => m.IsEnum ).Returns( true );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [] );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Enums.TestValue" );
        typeMock.Setup( m => m.Namespace ).Returns( "Rock.Enums" );

        Assert.True( EntityProperty.IsSupportedPropertyType( typeMock.Object ) );
    }

    [Fact]
    public void IsSupportedPropertyType_WithRockEnumDomainType_ReturnsTrue()
    {
        var attributeTypeMock = new Mock<Type>( MockBehavior.Strict );
        attributeTypeMock.Setup( m => m.FullName ).Returns( "Rock.Enums.EnumDomainAttribute" );

        var attributeMock = new Mock<CustomAttributeData>( MockBehavior.Strict );
        attributeMock.Setup( m => m.AttributeType ).Returns( attributeTypeMock.Object );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.IsGenericType ).Returns( false );
        typeMock.Setup( m => m.IsEnum ).Returns( true );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [] );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Data.TestValue" );
        typeMock.Setup( m => m.Namespace ).Returns( "Rock.Data" );
        typeMock.Setup( m => m.GetCustomAttributesData() ).Returns( [attributeMock.Object] );

        Assert.True( EntityProperty.IsSupportedPropertyType( typeMock.Object ) );
    }

    #endregion

    #region GetConvertToBagCode

    [Fact]
    public void GetConvertToBagCode_WithString_ReturnsPropertyName()
    {
        var propertyInfoMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyInfoMock.Setup( m => m.PropertyType ).Returns( typeof( string ) );
        propertyInfoMock.Setup( m => m.Name ).Returns( "Name" );

        var entityProperty = new EntityProperty( propertyInfoMock.Object );
        var code = entityProperty.ConvertToBagCode;

        Assert.Equal( "Name", code );
    }

    [Fact]
    public void GetConvertToBagCode_WithEntity_ReturnsToListItemBag()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.IsGenericType ).Returns( false );
        typeMock.Setup( m => m.IsEnum ).Returns( false );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );

        var propertyInfoMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyInfoMock.Setup( m => m.PropertyType ).Returns( typeMock.Object );
        propertyInfoMock.Setup( m => m.Name ).Returns( "DefinedValues" );

        var entityProperty = new EntityProperty( propertyInfoMock.Object );
        var code = entityProperty.ConvertToBagCode;

        Assert.Equal( "DefinedValues.ToListItemBag()", code );
    }

    [Fact]
    public void GetConvertToBagCode_WithEntityCollection_ReturnsToListItemBagList()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.IsGenericType ).Returns( false );
        typeMock.Setup( m => m.IsEnum ).Returns( false );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );

        var collectionTypeMock = new Mock<Type>( MockBehavior.Strict );
        collectionTypeMock.Setup( m => m.IsGenericType ).Returns( true );
        collectionTypeMock.Setup( m => m.IsEnum ).Returns( false );
        // For the current logic, the collect inner type doesn't matter.
        collectionTypeMock.Setup( m => m.GetInterfaces() ).Returns( [typeof( ICollection<string> )] );
        collectionTypeMock.Setup( m => m.GenericTypeArguments ).Returns( [typeMock.Object] );
        collectionTypeMock.Setup( m => m.FullName ).Returns( "Rock.FakeList" );

        var propertyInfoMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyInfoMock.Setup( m => m.PropertyType ).Returns( collectionTypeMock.Object );
        propertyInfoMock.Setup( m => m.Name ).Returns( "DefinedValues" );

        var entityProperty = new EntityProperty( propertyInfoMock.Object );
        var code = entityProperty.ConvertToBagCode;

        Assert.Equal( "DefinedValues.ToListItemBagList()", code );
    }

    [Fact]
    public void GetConvertToBagCode_WithNonCollectionGeneric_ReturnsUnknownPropertyType()
    {
        var propertyInfoMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyInfoMock.Setup( m => m.PropertyType ).Returns( typeof( NonCollectionGeneric<string> ) );
        propertyInfoMock.Setup( m => m.Name ).Returns( "DefinedValues" );

        var entityProperty = new EntityProperty( propertyInfoMock.Object );
        var code = entityProperty.ConvertToBagCode;

        Assert.Contains( "Unknown property type", code );
    }

    [Fact]
    public void GetConvertToBagCode_WithUnsupportedType_ThrowsException()
    {
        var propertyInfoMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyInfoMock.Setup( m => m.PropertyType ).Returns( typeof( EntityProperty ) );
        propertyInfoMock.Setup( m => m.Name ).Returns( "DefinedValue" );

        var entityProperty = new EntityProperty( propertyInfoMock.Object );

        var ex = Assert.Throws<Exception>( () => entityProperty.GetConvertToBagCode( true ) );

        Assert.Contains( "Unknown property type", ex.Message );
    }

    #endregion

    #region GetConvertFromBagCode

    [Fact]
    public void GetConvertFromBagCode_WithString_ReturnsPropertyName()
    {
        var propertyInfoMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyInfoMock.Setup( m => m.PropertyType ).Returns( typeof( string ) );
        propertyInfoMock.Setup( m => m.Name ).Returns( "Name" );

        var entityProperty = new EntityProperty( propertyInfoMock.Object );
        var code = entityProperty.ConvertFromBagCode;

        Assert.Equal( "Name", code );
    }

    [Fact]
    public void GetConvertFromBagCode_WithEntity_ReturnsGetEntityId()
    {
        var declaringTypeMock = new Mock<Type>( MockBehavior.Strict );
        declaringTypeMock.Protected()
            .Setup<PropertyInfo?>( "GetPropertyImpl", [
                "DefinedValueId",
                ItExpr.IsAny<BindingFlags>(),
                ItExpr.IsAny<Binder>(),
                ItExpr.IsAny<Type>(),
                ItExpr.IsAny<Type[]>(),
                ItExpr.IsAny<ParameterModifier[]>()
            ] )
            .Returns( ( PropertyInfo? ) null );

        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.IsGenericType ).Returns( false );
        typeMock.Setup( m => m.IsEnum ).Returns( false );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );
        typeMock.Setup( m => m.Name ).Returns( "DefinedValue" );

        var propertyInfoMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyInfoMock.Setup( m => m.DeclaringType ).Returns( declaringTypeMock.Object );
        propertyInfoMock.Setup( m => m.PropertyType ).Returns( typeMock.Object );
        propertyInfoMock.Setup( m => m.Name ).Returns( "MyProp" );

        var entityProperty = new EntityProperty( propertyInfoMock.Object );
        var code = entityProperty.ConvertFromBagCode;

        Assert.Equal( "MyProp.GetEntityId<DefinedValue>( RockContext )", code );
    }

    [Fact]
    public void GetConvertFromBagCode_WithEntityAndNonNullableId_ReturnsGetEntityId()
    {
        var idPropertyInfoMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        idPropertyInfoMock.Setup( m => m.PropertyType ).Returns( typeof( int ) );

        var declaringTypeMock = new Mock<Type>( MockBehavior.Strict );
        declaringTypeMock.Protected()
            .Setup<PropertyInfo?>( "GetPropertyImpl", [
                "DefinedValueId",
                ItExpr.IsAny<BindingFlags>(),
                ItExpr.IsAny<Binder>(),
                ItExpr.IsAny<Type>(),
                ItExpr.IsAny<Type[]>(),
                ItExpr.IsAny<ParameterModifier[]>()
            ] )
            .Returns( ( PropertyInfo? ) idPropertyInfoMock.Object );

        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.IsGenericType ).Returns( false );
        typeMock.Setup( m => m.IsEnum ).Returns( false );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );
        typeMock.Setup( m => m.Name ).Returns( "DefinedValue" );

        var propertyInfoMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyInfoMock.Setup( m => m.DeclaringType ).Returns( declaringTypeMock.Object );
        propertyInfoMock.Setup( m => m.PropertyType ).Returns( typeMock.Object );
        propertyInfoMock.Setup( m => m.Name ).Returns( "MyProp" );

        var entityProperty = new EntityProperty( propertyInfoMock.Object );
        var code = entityProperty.ConvertFromBagCode;

        Assert.Equal( "MyProp.GetEntityId<DefinedValue>( RockContext ).Value", code );
    }

    [Fact]
    public void GetConvertFromBagCode_WithEntityAndNullableId_ReturnsGetEntityId()
    {
        var idPropertyInfoMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        idPropertyInfoMock.Setup( m => m.PropertyType ).Returns( typeof( int? ) );

        var declaringTypeMock = new Mock<Type>( MockBehavior.Strict );
        declaringTypeMock.Protected()
            .Setup<PropertyInfo?>( "GetPropertyImpl", [
                "DefinedValueId",
                ItExpr.IsAny<BindingFlags>(),
                ItExpr.IsAny<Binder>(),
                ItExpr.IsAny<Type>(),
                ItExpr.IsAny<Type[]>(),
                ItExpr.IsAny<ParameterModifier[]>()
            ] )
            .Returns( ( PropertyInfo? ) idPropertyInfoMock.Object );

        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.IsGenericType ).Returns( false );
        typeMock.Setup( m => m.IsEnum ).Returns( false );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );
        typeMock.Setup( m => m.Name ).Returns( "DefinedValue" );

        var propertyInfoMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyInfoMock.Setup( m => m.DeclaringType ).Returns( declaringTypeMock.Object );
        propertyInfoMock.Setup( m => m.PropertyType ).Returns( typeMock.Object );
        propertyInfoMock.Setup( m => m.Name ).Returns( "MyProp" );

        var entityProperty = new EntityProperty( propertyInfoMock.Object );
        var code = entityProperty.ConvertFromBagCode;

        Assert.Equal( "MyProp.GetEntityId<DefinedValue>( RockContext )", code );
    }

    [Fact]
    public void GetConvertFromBagCode_WithUnsupportedType_ReturnsUnknownPropertyType()
    {
        var propertyInfoMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyInfoMock.Setup( m => m.PropertyType ).Returns( typeof( EntityProperty ) );
        propertyInfoMock.Setup( m => m.Name ).Returns( "DefinedValue" );

        var entityProperty = new EntityProperty( propertyInfoMock.Object );
        var code = entityProperty.ConvertFromBagCode;

        Assert.Contains( "Unknown property type", code );
    }

    [Fact]
    public void GetConvertFromBagCode_WithUnsupportedType_ThrowsException()
    {
        var propertyInfoMock = new Mock<PropertyInfo>( MockBehavior.Strict );
        propertyInfoMock.Setup( m => m.PropertyType ).Returns( typeof( EntityProperty ) );
        propertyInfoMock.Setup( m => m.Name ).Returns( "DefinedValue" );

        var entityProperty = new EntityProperty( propertyInfoMock.Object );

        var ex = Assert.Throws<Exception>( () => entityProperty.GetConvertFromBagCode( true ) );

        Assert.Contains( "Unknown property type", ex.Message );
    }

    #endregion

    private class NonCollectionGeneric<T>
    {
    }
}
