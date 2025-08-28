using System.Collections;
using System.Reflection;

using Moq;

namespace SparkDevNetwork.Rock.CodeGenerator.Tests;

public class ReflectionExtensionsTests
{
    #region GetCustomAttributeData

    [Fact]
    public void GetCustomAttributeData_WithAttribute_ReturnsAttributeData()
    {
        var member = typeof( ReflectionExtensionsTests ).GetMethod( nameof( GetCustomAttributeData_WithAttribute_ReturnsAttributeData ) );

        var attribute = member.GetCustomAttributeData( "Xunit.FactAttribute" );

        Assert.NotNull( attribute );
    }

    [Fact]
    public void GetCustomAttributeData_WithoutAttribute_ReturnsNull()
    {
        var member = typeof( ReflectionExtensionsTests ).GetMethod( nameof( GetCustomAttributeData_WithoutAttribute_ReturnsNull ) );

        var attribute = member.GetCustomAttributeData( "Xunit.MissingFactAttribute" );

        Assert.Null( attribute );
    }

    #endregion

    #region ImplementsInterface

    [Fact]
    public void ImplementsInterface_WithExplicitInterface_ReturnsTrue()
    {
        var type = typeof( BaseTestClassWithDisposable );

        var result = type.ImplementsInterface( typeof( IDisposable ).FullName );

        Assert.True( result );
    }

    [Fact]
    public void ImplementsInterface_WithInheritedInterface_ReturnsTrue()
    {
        var type = typeof( SubClassFromDisposable );

        var result = type.ImplementsInterface( typeof( IDisposable ).FullName );

        Assert.True( result );
    }

    [Fact]
    public void ImplementsInterface_WithGenericInterface_ReturnsTrue()
    {
        var type = typeof( TestClassWithGenericCollection );

        var result = type.ImplementsInterface( typeof( ICollection<> ).FullName );

        Assert.True( result );
    }

    [Fact]
    public void ImplementsInterface_WithMissingInterface_ReturnsFalse()
    {
        var type = typeof( SubClassFromDisposable );

        var result = type.ImplementsInterface( typeof( ICollection ).FullName );

        Assert.False( result );
    }

    #endregion

    #region GetFriendlyName

    [Fact]
    public void GetFriendlyName_WithNonGeneric_ReturnsName()
    {
        var name = ReflectionExtensions.GetFriendlyName( typeof( string ) );

        Assert.Equal( "String", name );
    }

    [Fact]
    public void GetFriendlyName_WithGeneric_ReturnsConciseName()
    {
        var name = ReflectionExtensions.GetFriendlyName( typeof( Dictionary<string, string> ) );

        Assert.Equal( "Dictionary<String, String>", name );
    }

    #endregion

    #region GetCSharpPropertyDeclaration

    [Theory]
    [InlineData( typeof( bool ), "bool" )]
    [InlineData( typeof( bool? ), "bool?" )]
    [InlineData( typeof( int ), "int" )]
    [InlineData( typeof( int? ), "int?" )]
    [InlineData( typeof( long ), "long" )]
    [InlineData( typeof( long? ), "long?" )]
    [InlineData( typeof( decimal ), "decimal" )]
    [InlineData( typeof( decimal? ), "decimal?" )]
    [InlineData( typeof( double ), "double" )]
    [InlineData( typeof( double? ), "double?" )]
    [InlineData( typeof( string ), "string" )]
    public void GetCSharpPropertyDeclaration_WithSimpleType_ReturnsNoUsings( Type type, string propertyType )
    {
        var declaration = ReflectionExtensions.GetCSharpPropertyDeclaration( type );

        Assert.Equal( propertyType, declaration.TypeName );
        Assert.Empty( declaration.RequiredUsings );
    }

    [Theory]
    [InlineData( typeof( Guid ), "Guid" )]
    [InlineData( typeof( Guid? ), "Guid?" )]
    [InlineData( typeof( DateTime ), "DateTime" )]
    [InlineData( typeof( DateTime? ), "DateTime?" )]
    [InlineData( typeof( DateTimeOffset ), "DateTimeOffset" )]
    [InlineData( typeof( DateTimeOffset? ), "DateTimeOffset?" )]
    public void GetCSharpPropertyDeclaration_WithSystemType_ReturnsSystemUsing( Type type, string propertyType )
    {
        var declaration = ReflectionExtensions.GetCSharpPropertyDeclaration( type );

        Assert.Equal( propertyType, declaration.TypeName );
        Assert.Single( declaration.RequiredUsings );
        Assert.Equal( "System", declaration.RequiredUsings[0] );
    }

    [Fact]
    public void GetCSharpPropertyDeclaration_WithRockEnum_ReturnsTypeNamespaceUsing()
    {
        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.IsGenericType ).Returns( false );
        typeMock.Setup( m => m.IsEnum ).Returns( true );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [] );
        typeMock.Setup( m => m.Name ).Returns( "TestValue" );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Enums.TestValue" );
        typeMock.Setup( m => m.Namespace ).Returns( "Rock.Enums" );

        var declaration = ReflectionExtensions.GetCSharpPropertyDeclaration( typeMock.Object );

        Assert.Equal( "TestValue", declaration.TypeName );
        Assert.Single( declaration.RequiredUsings );
        Assert.Equal( "Rock.Enums", declaration.RequiredUsings[0] );
    }

    [Fact]
    public void GetCSharpPropertyDeclaration_WithPluginEnum_ReturnsTypeNamespaceUsing()
    {
        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.IsGenericType ).Returns( false );
        typeMock.Setup( m => m.IsEnum ).Returns( true );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [] );
        typeMock.Setup( m => m.Name ).Returns( "TestValue" );
        typeMock.Setup( m => m.FullName ).Returns( "Plugin.Enums.TestValue" );
        typeMock.Setup( m => m.Namespace ).Returns( "Plugin.Enums" );
        typeMock.Setup( m => m.GetCustomAttributesData() ).Returns( [] );

        var declaration = ReflectionExtensions.GetCSharpPropertyDeclaration( typeMock.Object );

        Assert.Equal( "TestValue", declaration.TypeName );
        Assert.Single( declaration.RequiredUsings );
        Assert.Equal( "Plugin.Enums", declaration.RequiredUsings[0] );
    }

    [Fact]
    public void GetCSharpPropertyDeclaration_WithRockDomainEnum_ReturnsTypeNamespaceUsing()
    {
        var attributeTypeMock = new Mock<Type>( MockBehavior.Strict );
        attributeTypeMock.Setup( m => m.FullName ).Returns( "Rock.Enums.EnumDomainAttribute" );

        var attributeMock = new Mock<CustomAttributeData>( MockBehavior.Strict );
        attributeMock.Setup( m => m.AttributeType ).Returns( attributeTypeMock.Object );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.IsGenericType ).Returns( false );
        typeMock.Setup( m => m.IsEnum ).Returns( true );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [] );
        typeMock.Setup( m => m.Name ).Returns( "TestValue" );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Data.TestValue" );
        typeMock.Setup( m => m.Namespace ).Returns( "Rock.Data" );
        typeMock.Setup( m => m.GetCustomAttributesData() ).Returns( [attributeMock.Object] );

        var declaration = ReflectionExtensions.GetCSharpPropertyDeclaration( typeMock.Object );

        Assert.Equal( "TestValue", declaration.TypeName );
        Assert.Single( declaration.RequiredUsings );
        Assert.Equal( "Rock.Data", declaration.RequiredUsings[0] );
    }

    [Fact]
    public void GetCSharpPropertyDeclaration_WithUnsupportedEnum_ThrowsException()
    {
        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.IsGenericType ).Returns( false );
        typeMock.Setup( m => m.IsEnum ).Returns( true );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [] );
        typeMock.Setup( m => m.Name ).Returns( "TestValue" );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Data.TestValue" );
        typeMock.Setup( m => m.Namespace ).Returns( "Rock.Data" );
        typeMock.Setup( m => m.GetCustomAttributesData() ).Returns( [] );

        Assert.Throws<Exception>( () => ReflectionExtensions.GetCSharpPropertyDeclaration( typeMock.Object ) );
    }

    #endregion

    #region IsRockEntity

    [Fact]
    public void IsRockWith_WithEntity_ReturnsTrue()
    {
        var entityInterfaceMock = new Mock<Type>( MockBehavior.Strict );
        entityInterfaceMock.Setup( m => m.IsGenericType ).Returns( false );
        entityInterfaceMock.Setup( m => m.FullName ).Returns( "Rock.Data.IEntity" );

        var typeMock = new Mock<Type>( MockBehavior.Strict );
        typeMock.Setup( m => m.IsGenericType ).Returns( false );
        typeMock.Setup( m => m.IsEnum ).Returns( false );
        typeMock.Setup( m => m.GetInterfaces() ).Returns( [entityInterfaceMock.Object] );
        typeMock.Setup( m => m.FullName ).Returns( "Rock.Model.DefinedValue" );

        Assert.True( ReflectionExtensions.IsRockEntity( typeMock.Object ) );
    }

    [Fact]
    public void IsRockWith_WithNonEntity_ReturnsTrue()
    {
        Assert.False( ReflectionExtensions.IsRockEntity( typeof( string ) ) );
    }

    #endregion

    #region Support Classes

    private class BaseTestClassWithDisposable : IDisposable, IEnumerable<int>
    {
        public void Dispose()
        {
        }

        public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }

    private class SubClassFromDisposable : BaseTestClassWithDisposable
    {
    }

    private class TestClassWithGenericCollection : ICollection<int>
    {
        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add( int item ) => throw new NotImplementedException();

        public void Clear() => throw new NotImplementedException();

        public bool Contains( int item ) => throw new NotImplementedException();

        public void CopyTo( int[] array, int arrayIndex ) => throw new NotImplementedException();

        public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();

        public bool Remove( int item ) => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }

    #endregion
}
