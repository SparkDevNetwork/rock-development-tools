using System.Reflection;

using Moq;
using Moq.Protected;

using SparkDevNetwork.Rock.CodeGenerator.ListBlock;

namespace SparkDevNetwork.Rock.CodeGenerator.Tests.ListBlock;

public class EntityColumnTests
{
    #region IsPrimitiveType

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
    public void IsPrimitiveType_WithPrimitiveTypes_ReturnsTrue( Type type )
    {
        Assert.True( EntityColumn.IsPrimitiveType( type ), $"Expected type '{type.FullName}' was not considered primitive." );
    }

    [Theory]
    [InlineData( typeof( DateTime ) )]
    [InlineData( typeof( EntityColumn ) )]
    public void IsPrimitiveType_WithNonPrimitiveTypes_ReturnsFalse( Type type )
    {
        Assert.False( EntityColumn.IsPrimitiveType( type ), $"Unexpected type '{type.FullName}' was considered primitive." );
    }

    #endregion

    #region IsNumericType

    [Theory]
    [InlineData( typeof( int ) )]
    [InlineData( typeof( int? ) )]
    [InlineData( typeof( long ) )]
    [InlineData( typeof( long? ) )]
    [InlineData( typeof( decimal ) )]
    [InlineData( typeof( decimal? ) )]
    [InlineData( typeof( double ) )]
    [InlineData( typeof( double? ) )]
    public void IsNumericType_WithNumericTypes_ReturnsTrue( Type type )
    {
        Assert.True( EntityColumn.IsNumericType( type ), $"Expected type '{type.FullName}' was not considered numeric." );
    }

    [Theory]
    [InlineData( typeof( string ) )]
    [InlineData( typeof( DateTime ) )]
    [InlineData( typeof( EntityColumn ) )]
    public void IsNumericType_WithNonNumericTypes_ReturnsFalse( Type type )
    {
        Assert.False( EntityColumn.IsNumericType( type ), $"Unexpected type '{type.FullName}' was considered numeric." );
    }

    #endregion

    #region IsDateType

    [Theory]
    [InlineData( typeof( DateTime ) )]
    [InlineData( typeof( DateTime? ) )]
    [InlineData( typeof( DateTimeOffset ) )]
    [InlineData( typeof( DateTimeOffset? ) )]
    public void IsDateType_WithDateTypes_ReturnsTrue( Type type )
    {
        Assert.True( EntityColumn.IsDateType( type ), $"Expected type '{type.FullName}' was not considered date." );
    }

    [Theory]
    [InlineData( typeof( string ) )]
    [InlineData( typeof( int ) )]
    [InlineData( typeof( EntityColumn ) )]
    public void IsDateType_WithNonDateTypes_ReturnsFalse( Type type )
    {
        Assert.False( EntityColumn.IsDateType( type ), $"Unexpected type '{type.FullName}' was considered date." );
    }

    #endregion

    #region IsSupportedPropertyType

    [Fact]
    public void IsSupportedPropertyType_WithEntityType_ReturnsTrue()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        var typeMock = new Mock<Type>( MockBehavior.Strict );

        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );

        Assert.True( EntityColumn.IsSupportedPropertyType( typeMock.Object ) );
    }

    [Fact]
    public void IsSupportedPropertyType_WithNumericType_ReturnsTrue()
    {
        Assert.True( EntityColumn.IsSupportedPropertyType( typeof( int ) ) );
    }

    [Fact]
    public void IsSupportedPropertyType_WithDateType_ReturnsTrue()
    {
        Assert.True( EntityColumn.IsSupportedPropertyType( typeof( DateTimeOffset ) ) );
    }

    [Fact]
    public void IsSupportedPropertyType_WithPrimitiveType_ReturnsTrue()
    {
        Assert.True( EntityColumn.IsSupportedPropertyType( typeof( string ) ) );
    }

    [Fact]
    public void IsSupportedPropertyType_WithUnsupportedType_ReturnsFalse()
    {
        Assert.False( EntityColumn.IsSupportedPropertyType( typeof( EntityColumn ) ) );
    }

    #endregion

    #region GetGridImports

    [Theory]
    [InlineData( typeof( string ) )]
    [InlineData( typeof( Guid ) )]
    [InlineData( typeof( Guid? ) )]
    public void GetGridImports_TextTypes_ReturnsTextColumnValues( Type type )
    {
        var entityColumn = CreateTestEntityColumn( type );
        var imports = entityColumn.GridImports.ToArray();

        Assert.Equal( 2, imports.Length );
        Assert.Contains( "TextColumn", imports );
        Assert.Contains( "textValueFilter", imports );
    }

    [Fact]
    public void GetGridImports_NumericTypes_ReturnsNumberColumnValues()
    {
        var entityColumn = CreateTestEntityColumn( typeof( int ) );
        var imports = entityColumn.GridImports.ToArray();

        Assert.Equal( 2, imports.Length );
        Assert.Contains( "NumberColumn", imports );
        Assert.Contains( "numberValueFilter", imports );
    }

    [Theory]
    [InlineData( typeof( bool ) )]
    [InlineData( typeof( bool? ) )]
    public void GetGridImports_BooleanTypes_ReturnsBooleanColumnValues( Type type )
    {
        var entityColumn = CreateTestEntityColumn( type );
        var imports = entityColumn.GridImports.ToArray();

        Assert.Single( imports );
        Assert.Contains( "BooleanColumn", imports );
    }

    [Fact]
    public void GetGridImports_DateTypes_ReturnsDateColumnValues()
    {
        var entityColumn = CreateTestEntityColumn( typeof( DateTimeOffset ) );
        var imports = entityColumn.GridImports.ToArray();

        Assert.Equal( 2, imports.Length );
        Assert.Contains( "DateColumn", imports );
        Assert.Contains( "dateValueFilter", imports );
    }

    [Fact]
    public void GetGridImports_PersonAliasType_ReturnsPersonColumnValues()
    {
        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.PersonAlias" );

        var entityColumn = CreateTestEntityColumn( typeMock.Object );
        var imports = entityColumn.GridImports.ToArray();

        Assert.Equal( 2, imports.Length );
        Assert.Contains( "PersonColumn", imports );
        Assert.Contains( "pickExistingValueFilter", imports );
    }

    [Fact]
    public void GetGridImports_EntityTypes_ReturnsTextColumnValues()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );

        var entityColumn = CreateTestEntityColumn( typeMock.Object );
        var imports = entityColumn.GridImports.ToArray();

        Assert.Equal( 2, imports.Length );
        Assert.Contains( "TextColumn", imports );
        Assert.Contains( "textValueFilter", imports );
    }

    [Fact]
    public void GetGridImports_UnknownType_ReturnsColumnValues()
    {
        var entityColumn = CreateTestEntityColumn( typeof( EntityColumn ) );
        var imports = entityColumn.GridImports.ToArray();

        Assert.Single( imports );
        Assert.Contains( "Column", imports );
    }

    #endregion

    #region GetTemplateCode

    [Theory]
    [InlineData( typeof( string ) )]
    [InlineData( typeof( Guid ) )]
    [InlineData( typeof( Guid? ) )]
    public void GetTemplateCode_TextTypes_ReturnsTextColumn( Type type )
    {
        var entityColumn = CreateTestEntityColumn( type );
        var code = entityColumn.TemplateCode;

        Assert.Contains( "<TextColumn ", code );
    }

    [Fact]
    public void GetTemplateCode_NumericType_ReturnsNumberColumn()
    {
        var entityColumn = CreateTestEntityColumn( typeof( int ) );
        var code = entityColumn.TemplateCode;

        Assert.Contains( "<NumberColumn ", code );
    }

    [Theory]
    [InlineData( typeof( bool ) )]
    [InlineData( typeof( bool? ) )]
    public void GetTemplateCode_BooleanTypes_ReturnsBooleanColumn( Type type )
    {
        var entityColumn = CreateTestEntityColumn( type );
        var code = entityColumn.TemplateCode;

        Assert.Contains( "<BooleanColumn ", code );
    }

    [Fact]
    public void GetTemplateCode_DateType_ReturnsDateColumn()
    {
        var entityColumn = CreateTestEntityColumn( typeof( DateTimeOffset ) );
        var code = entityColumn.TemplateCode;

        Assert.Contains( "<DateColumn ", code );
    }

    [Fact]
    public void GetTemplateCode_PersonAliasType_ReturnsPersonColumn()
    {
        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.PersonAlias" );

        var entityColumn = CreateTestEntityColumn( typeMock.Object );
        var code = entityColumn.TemplateCode;

        Assert.Contains( "<PersonColumn ", code );
    }

    [Fact]
    public void GetTemplateCode_EntityType_ReturnsTextColumn()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );

        var entityColumn = CreateTestEntityColumn( typeMock.Object );
        var code = entityColumn.TemplateCode;

        Assert.Contains( "<TextColumn ", code );
    }

    [Fact]
    public void GetTemplateCode_UnknownType_ReturnsColumn()
    {
        var entityColumn = CreateTestEntityColumn( typeof( EntityColumn ) );
        var code = entityColumn.TemplateCode;

        Assert.Contains( "<Column ", code );
    }

    #endregion

    #region GetAddFieldCode

    [Fact]
    public void GetAddFieldCode_DateType_ReturnsDateTimeField()
    {
        var entityColumn = CreateTestEntityColumn( typeof( DateTimeOffset ) );
        var code = entityColumn.AddFieldCode;

        Assert.Contains( ".AddDateTimeField(", code );
    }

    [Fact]
    public void GetAddFieldCode_PersonAliasType_ReturnsPersonField()
    {
        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.PersonAlias" );

        var entityColumn = CreateTestEntityColumn( typeMock.Object );
        var code = entityColumn.AddFieldCode;

        Assert.Contains( ".AddPersonField(", code );
    }

    [Fact]
    public void GetAddFieldCode_EntityType_ReturnsTextField()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.TestModel" );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );
        typeMock.Protected()
            .Setup<PropertyInfo?>( "GetPropertyImpl", [
                "Name",
                ItExpr.IsAny<BindingFlags>(),
                ItExpr.IsAny<Binder>(),
                ItExpr.IsAny<Type>(),
                ItExpr.IsAny<Type[]>(),
                ItExpr.IsAny<ParameterModifier[]>()
            ] )
            .Returns( ( PropertyInfo? ) null );
        typeMock.Protected()
            .Setup<PropertyInfo?>( "GetPropertyImpl", [
                "Title",
                ItExpr.IsAny<BindingFlags>(),
                ItExpr.IsAny<Binder>(),
                ItExpr.IsAny<Type>(),
                ItExpr.IsAny<Type[]>(),
                ItExpr.IsAny<ParameterModifier[]>()
            ] )
            .Returns( ( PropertyInfo? ) null );

        var entityColumn = CreateTestEntityColumn( typeMock.Object );
        var code = entityColumn.AddFieldCode;

        Assert.Contains( ".AddTextField(", code );
    }

    [Fact]
    public void GetAddFieldCode_EntityTypeWithName_ReturnsNameProperty()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.TestModel" );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );
        typeMock.Protected()
            .Setup<PropertyInfo?>( "GetPropertyImpl", [
                "Name",
                ItExpr.IsAny<BindingFlags>(),
                ItExpr.IsAny<Binder>(),
                ItExpr.IsAny<Type>(),
                ItExpr.IsAny<Type[]>(),
                ItExpr.IsAny<ParameterModifier[]>()
            ] )
            .Returns( new Mock<PropertyInfo>( MockBehavior.Strict ).Object );
        typeMock.Protected()
            .Setup<PropertyInfo?>( "GetPropertyImpl", [
                "Title",
                ItExpr.IsAny<BindingFlags>(),
                ItExpr.IsAny<Binder>(),
                ItExpr.IsAny<Type>(),
                ItExpr.IsAny<Type[]>(),
                ItExpr.IsAny<ParameterModifier[]>()
            ] )
            .Returns( ( PropertyInfo? ) null );

        var entityColumn = CreateTestEntityColumn( typeMock.Object );
        var code = entityColumn.AddFieldCode;

        Assert.Contains( "a.Test?.Name", code );
    }

    [Fact]
    public void GetAddFieldCode_EntityTypeWithTitle_ReturnsTitleProperty()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.TestModel" );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );
        typeMock.Protected()
            .Setup<PropertyInfo?>( "GetPropertyImpl", [
                "Name",
                ItExpr.IsAny<BindingFlags>(),
                ItExpr.IsAny<Binder>(),
                ItExpr.IsAny<Type>(),
                ItExpr.IsAny<Type[]>(),
                ItExpr.IsAny<ParameterModifier[]>()
            ] )
            .Returns( ( PropertyInfo? ) null );
        typeMock.Protected()
            .Setup<PropertyInfo?>( "GetPropertyImpl", [
                "Title",
                ItExpr.IsAny<BindingFlags>(),
                ItExpr.IsAny<Binder>(),
                ItExpr.IsAny<Type>(),
                ItExpr.IsAny<Type[]>(),
                ItExpr.IsAny<ParameterModifier[]>()
            ] )
            .Returns( new Mock<PropertyInfo>( MockBehavior.Strict ).Object );

        var entityColumn = CreateTestEntityColumn( typeMock.Object );
        var code = entityColumn.AddFieldCode;

        Assert.Contains( "a.Test?.Title", code );
    }

    [Fact]
    public void GetAddFieldCode_DefinedValueType_ReturnsValueProperty()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );
        typeMock.Protected()
            .Setup<PropertyInfo?>( "GetPropertyImpl", [
                "Name",
                ItExpr.IsAny<BindingFlags>(),
                ItExpr.IsAny<Binder>(),
                ItExpr.IsAny<Type>(),
                ItExpr.IsAny<Type[]>(),
                ItExpr.IsAny<ParameterModifier[]>()
            ] )
            .Returns( ( PropertyInfo? ) null );
        typeMock.Protected()
            .Setup<PropertyInfo?>( "GetPropertyImpl", [
                "Title",
                ItExpr.IsAny<BindingFlags>(),
                ItExpr.IsAny<Binder>(),
                ItExpr.IsAny<Type>(),
                ItExpr.IsAny<Type[]>(),
                ItExpr.IsAny<ParameterModifier[]>()
            ] )
            .Returns( ( PropertyInfo? ) null );

        var entityColumn = CreateTestEntityColumn( typeMock.Object );
        var code = entityColumn.AddFieldCode;

        Assert.Contains( "a.Test?.Value", code );
    }

    [Fact]
    public void GetAddFieldCode_StringType_ReturnsTextField()
    {
        var entityColumn = CreateTestEntityColumn( typeof( string ) );
        var code = entityColumn.AddFieldCode;

        Assert.Contains( ".AddTextField(", code );
    }

    [Fact]
    public void GetAddFieldCode_PrimitiveType_ReturnsField()
    {
        var entityColumn = CreateTestEntityColumn( typeof( bool ) );
        var code = entityColumn.AddFieldCode;

        Assert.Contains( ".AddField(", code );
    }

    [Fact]
    public void GetAddFieldCode_UnknownType_ReturnsFieldWithThrow()
    {
        var entityColumn = CreateTestEntityColumn( typeof( EntityColumn ) );
        var code = entityColumn.AddFieldCode;

        Assert.Contains( ".AddField(", code );
        Assert.Contains( "throw new NotSupportedException()", code );
    }

    #endregion

    #region FriendlyName

    [Fact]
    public void FriendlyName_WithDefinedValue_StripsValueSuffix()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );

        var entityColumn = CreateTestEntityColumn( typeMock.Object, "TestValue" );
        var name = entityColumn.FriendlyName;

        Assert.Equal( "Test", name );
    }

    [Fact]
    public void FriendlyName_WithPersonAlias_StripsPersonAliasSuffix()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.PersonAlias" );

        var entityColumn = CreateTestEntityColumn( typeMock.Object, "TestPersonAlias" );
        var name = entityColumn.FriendlyName;

        Assert.Equal( "Test", name );
    }

    [Fact]
    public void FriendlyName_WithPersonAlias_StripsAliasSuffix()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.PersonAlias" );

        var entityColumn = CreateTestEntityColumn( typeMock.Object, "TestAlias" );
        var name = entityColumn.FriendlyName;

        Assert.Equal( "Test", name );
    }

    #endregion

    #region Helper Methods

    private static EntityColumn CreateTestEntityColumn( Type propertyType, string propertyName = "Test" )
    {
        var propertyInfoMock = new Mock<PropertyInfo>( MockBehavior.Strict );

        propertyInfoMock.Setup( m => m.PropertyType ).Returns( propertyType );
        propertyInfoMock.Setup( m => m.Name ).Returns( propertyName );

        return new EntityColumn( propertyInfoMock.Object );
    }

    #endregion
}
