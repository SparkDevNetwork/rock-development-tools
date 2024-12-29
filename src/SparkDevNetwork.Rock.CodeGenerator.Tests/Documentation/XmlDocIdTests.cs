using System.Reflection;

using Moq;
using Moq.Protected;

using SparkDevNetwork.Rock.CodeGenerator.Documentation;

namespace SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation;

public class XmlDocIdTests
{
    #region TypeId

    [Theory]
    [InlineData( typeof( string ), "T:System.String" )]
    [InlineData( typeof( List<string> ), "T:System.Collections.Generic.List`1" )]
    [InlineData( typeof( List<> ), "T:System.Collections.Generic.List`1" )]
    [InlineData( typeof( int[,] ), "T:System.Int32[0:,0:]" )]
    [InlineData( typeof( int? ), "T:System.Nullable{System.Int32}" )]
    public void TypeId_StandardTypes_ReturnsCorrectId( Type type, string expectedId )
    {
        var id = XmlDocId.TypeId( type );

        Assert.Equal( expectedId, id );
    }

    #endregion

    #region GetTypeXmlId

    [Fact]
    public void GetTypeXmlId_WithNamespace_ReturnsNamespaceAndTypeName()
    {
        var mock = GetTypeMock( "TestLib", "Root" );

        var id = XmlDocId.GetTypeXmlId( mock.Object );

        Assert.Equal( "TestLib.Root", id );
    }

    [Fact]
    public void GetTypeXmlId_WithoutNamespace_ReturnsTypeName()
    {
        var mock = GetTypeMock( null, "Root" );

        mock.Setup( m => m.Namespace ).Returns( ( string? ) null );

        var id = XmlDocId.GetTypeXmlId( mock.Object );

        Assert.Equal( "Root", id );
    }

    [Fact]
    public void GetTypeXmlId_WithNestedType_ReturnsPrefixedId()
    {
        var mock = GetTypeMock( "TestLib", "Root" );
        var nestedTypeMock = GetTypeMock( "TestLib", "Nested" );

        nestedTypeMock.Setup( m => m.DeclaringType ).Returns( mock.Object );

        var id = XmlDocId.GetTypeXmlId( nestedTypeMock.Object );

        Assert.Equal( "TestLib.Root.Nested", id );
    }

    [Fact]
    public void GetTypeXmlId_WithArrayGenericParameters_ReturnsArrayTypeName()
    {
        var mock = GetTypeMock();

        mock.Setup( m => m.Namespace ).Returns( ( string? ) null );
        mock.Setup( m => m.ContainsGenericParameters ).Returns( true );
        mock.Protected().Setup<bool>( "IsArrayImpl" ).Returns( true );
        mock.Setup( m => m.GetElementType() ).Returns( typeof( string ) );

        var id = XmlDocId.GetTypeXmlId( mock.Object );

        Assert.Equal( "System.String[]", id );
    }

    [Fact]
    public void GetTypeXmlId_WithGenericParameters_ReturnsInnerTypeName()
    {
        var mock = GetTypeMock();

        mock.Setup( m => m.Namespace ).Returns( ( string? ) null );
        mock.Setup( m => m.ContainsGenericParameters ).Returns( true );
        mock.Protected().Setup<bool>( "IsArrayImpl" ).Returns( false );
        mock.Setup( m => m.GetElementType() ).Returns( typeof( string ) );

        var id = XmlDocId.GetTypeXmlId( mock.Object );

        Assert.Equal( "System.String", id );
    }

    [Fact]
    public void GetTypeXmlId_WithGenericParameterAndNullClassParams_Returns()
    {
        var mock = GetTypeMock();

        mock.Setup( m => m.Namespace ).Returns( ( string? ) null );
        mock.Setup( m => m.IsGenericParameter ).Returns( true );
        mock.Setup( m => m.GenericParameterPosition ).Returns( 3 );

        var id = XmlDocId.GetTypeXmlId( mock.Object, genericClassParams: null );

        Assert.Equal( "``3", id );
    }

    [Fact]
    public void GetTypeXmlId_WithGenericParameterAndEmptyClassParams_ReturnsDoubleBackticks()
    {
        var mock = GetTypeMock( "TestLib", "Root" );

        mock.Setup( m => m.IsGenericParameter ).Returns( true );
        mock.Setup( m => m.GenericParameterPosition ).Returns( 3 );

        var id = XmlDocId.GetTypeXmlId( mock.Object, genericClassParams: [] );

        Assert.Equal( "``3", id );
    }

    [Fact]
    public void GetTypeXmlId_WithGenericParameterAndClassParams_ReturnsSingleBacktick()
    {
        var mock = GetTypeMock( "TestLib", "Root" );

        mock.Setup( m => m.IsGenericParameter ).Returns( true );
        mock.Setup( m => m.GenericParameterPosition ).Returns( 3 );

        var id = XmlDocId.GetTypeXmlId( mock.Object, genericClassParams: ["Root"] );

        Assert.Equal( "`3", id );
    }

    [Fact]
    public void GetTypeXmlId_WithOutputParameter_ReturnsOutputSuffix()
    {
        var mock = GetTypeMock( "TestLib", "Root" );

        var id = XmlDocId.GetTypeXmlId( mock.Object, isOut: true );

        Assert.Equal( "TestLib.Root@", id );
    }

    #endregion

    #region PropertyId

    [Fact]
    public void PropertyId_WithNull_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>( () => XmlDocId.PropertyId( null ) );
    }

    [Fact]
    public void PropertyId_WithNonProperty_ThrowsException()
    {
        var member = typeof( TestClass ).GetField( nameof( TestClass.IntField ) );

        Assert.Throws<ArgumentException>( () => XmlDocId.PropertyId( member ) );
    }

    [Fact]
    public void PropertyId_WithNormalProperty_ReturnsId()
    {
        var member = typeof( TestClass ).GetProperty( nameof( TestClass.IntProperty ) );

        var id = XmlDocId.PropertyId( member );

        Assert.Equal( "P:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass.IntProperty", id );
    }

    [Fact]
    public void PropertyId_WithNamedItemProperty_ReturnsId()
    {
        var member = typeof( TestClass ).GetProperty( nameof( TestClass.Item ) );

        var id = XmlDocId.PropertyId( member );

        Assert.Equal( "P:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass.Item", id );
    }

    [Fact]
    public void PropertyId_WithItemGetterProperty_ReturnsId()
    {
        var member = typeof( TestClassItemWithGetter ).GetProperty( "Item" );

        var id = XmlDocId.PropertyId( member );

        Assert.Equal( "P:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClassItemWithGetter.Item(System.Int32)", id );
    }

    [Fact]
    public void PropertyId_WithItemSetterOnlyProperty_ReturnsId()
    {
        var member = typeof( TestClassItemWithoutGetter ).GetProperty( "Item" );

        var id = XmlDocId.PropertyId( member );

        Assert.Equal( "P:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClassItemWithoutGetter.Item(System.Int32)", id );
    }

    [Fact]
    public void PropertyId_WithItemPropertyWithNoGetterOrSetter_ReturnsId()
    {
        var member = new Mock<PropertyInfo>( MockBehavior.Strict );

        member.Setup( m => m.MemberType ).Returns( MemberTypes.Property );
        member.Setup( m => m.Name ).Returns( "Item" );
        member.Setup( m => m.DeclaringType ).Returns( typeof( TestClass ) );
        member.Setup( m => m.GetMethod ).Returns( ( MethodInfo? ) null );
        member.Setup( m => m.SetMethod ).Returns( ( MethodInfo? ) null );

        var id = XmlDocId.PropertyId( member.Object );

        Assert.Equal( "P:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass.Item", id );
    }

    [Fact]
    public void PropertyId_WithMemberInfoItem_ReturnsId()
    {
        var member = new Mock<MemberInfo>( MockBehavior.Strict );

        member.Setup( m => m.MemberType ).Returns( MemberTypes.Property );
        member.Setup( m => m.Name ).Returns( "Item" );
        member.Setup( m => m.DeclaringType ).Returns( typeof( TestClass ) );

        var id = XmlDocId.PropertyId( member.Object );

        Assert.Equal( "P:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass.Item", id );
    }

    #endregion

    #region FieldId

    [Fact]
    public void FieldId_WithNull_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>( () => XmlDocId.FieldId( null ) );
    }

    [Fact]
    public void FieldId_WithNonField_ThrowsException()
    {
        var member = typeof( TestClass ).GetProperty( nameof( TestClass.IntProperty ) );

        Assert.Throws<ArgumentException>( () => XmlDocId.FieldId( member ) );
    }

    [Fact]
    public void FieldId_WithField_ReturnsId()
    {
        var member = typeof( TestClass ).GetField( nameof( TestClass.IntField ) );

        var id = XmlDocId.FieldId( member );

        Assert.Equal( "F:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass.IntField", id );
    }

    #endregion

    #region EventId

    [Fact]
    public void EventId_WithNull_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>( () => XmlDocId.EventId( null ) );
    }

    [Fact]
    public void EventId_WithNonEvent_ThrowsException()
    {
        var member = typeof( TestClass ).GetProperty( nameof( TestClass.IntProperty ) );

        Assert.Throws<ArgumentException>( () => XmlDocId.EventId( member ) );
    }

    [Fact]
    public void EventId_WithEvent_ReturnsId()
    {
        var member = typeof( TestClass ).GetEvent( nameof( TestClass.Event ) );

        var id = XmlDocId.EventId( member );

        Assert.Equal( "E:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass.Event", id );
    }

    #endregion

    #region EnumValueId

    [Fact]
    public void EnumValueId_WithNullType_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>( () => XmlDocId.EnumValueId( null, null ) );
    }

    [Fact]
    public void EnumValueId_WithNullName_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>( () => XmlDocId.EnumValueId( typeof( TestEnum ), null ) );
    }

    [Fact]
    public void EnumValueId_WithNonEnum_ThrowsException()
    {
        var type = typeof( TestClass );

        Assert.Throws<ArgumentException>( () => XmlDocId.EnumValueId( type, "Zero" ) );
    }

    [Fact]
    public void EnumValueId_WithEnumName_ReturnsId()
    {
        var type = typeof( TestEnum );
        var name = nameof( TestEnum.Zero );

        var id = XmlDocId.EnumValueId( type, name );

        Assert.Equal( "F:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestEnum.Zero", id );
    }

    #endregion

    #region MethodId

    [Fact]
    public void MethodId_WithNull_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>( () => XmlDocId.MethodId( null ) );
    }

    [Fact]
    public void MethodId_WithNormalMethod_ReturnsId()
    {
        var member = typeof( TestClass ).GetMethod( nameof( TestClass.Add ) );

        var id = XmlDocId.MethodId( member );

        Assert.Equal( "M:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass.Add(System.Int32,System.Int32)", id );
    }

    [Fact]
    public void MethodId_WithIndexerGetMethod_ReturnsId()
    {
        var member = typeof( TestClass ).GetMethod( "get_Item" );

        var id = XmlDocId.MethodId( member );

        Assert.Equal( "P:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass.Item", id );
    }

    [Fact]
    public void MethodId_WithIndexerSetMethod_ReturnsId()
    {
        var member = typeof( TestClass ).GetMethod( "set_Item" );

        var id = XmlDocId.MethodId( member );

        Assert.Equal( "P:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass.Item(System.Int32)", id );
    }

    [Fact]
    public void MethodId_WithExplicitConversionMethod_ReturnsId()
    {
        var member = typeof( TestClass ).GetMethod( "op_Explicit" );

        var id = XmlDocId.MethodId( member );

        Assert.Equal( "M:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass.op_Explicit(System.Int32)~SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass", id );
    }

    [Fact]
    public void MethodId_WithImplicitConversionMethod_ReturnsId()
    {
        var member = typeof( TestClass ).GetMethod( "op_Implicit" );

        var id = XmlDocId.MethodId( member );

        Assert.Equal( "M:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass.op_Implicit(SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass)~System.Int32", id );
    }

    [Fact]
    public void MethodId_WithConstructor_ReturnsId()
    {
        var member = typeof( TestClass ).GetConstructor( [typeof( int )] );

        var id = XmlDocId.MethodId( member );

        Assert.Equal( "M:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass.#ctor(System.Int32)", id );
    }

    [Fact]
    public void MethodId_WithPropertyMethod_ReturnsId()
    {
        var member = typeof( TestClass ).GetProperty( nameof( TestClass.IntProperty ) );

        var id = XmlDocId.MethodId( member!.GetMethod );

        Assert.Equal( "M:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass.get_IntProperty", id );
    }

    [Fact]
    public void MethodId_WithGenericMethod_ReturnsId()
    {
        var member = typeof( TestClass ).GetMethod( nameof( TestClass.PrintGeneric ) );
        var method = member?.MakeGenericMethod( typeof( int ) );

        var id = XmlDocId.MethodId( method );

        Assert.Equal( "M:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass.PrintGeneric``1(System.Int32)", id );
    }

    [Fact]
    public void MethodId_WithTypeGenericMethod_ReturnsId()
    {
        var method = typeof( List<int> ).GetMethod( nameof( List<int>.Add ) );

        var id = XmlDocId.MethodId( method );

        Assert.Equal( "M:System.Collections.Generic.List`1.Add(System.Int32)", id );
    }

    [Fact]
    public void MethodId_WithRefParameter_ReturnsId()
    {
        var method = typeof( TestClass ).GetMethod( nameof( TestClass.ByRef ) );

        var id = XmlDocId.MethodId( method );

        Assert.Equal( "M:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass.ByRef(System.Int32@)", id );
    }

    [Fact]
    public void MethodId_WithOutParameter_ReturnsId()
    {
        var method = typeof( TestClass ).GetMethod( nameof( TestClass.Out ) );

        var id = XmlDocId.MethodId( method );

        Assert.Equal( "M:SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation.XmlDocIdTests.TestClass.Out(System.Int32@)", id );
    }

    #endregion

    #region MemberId

    [Fact]
    public void MemberId_WithNull_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>( () => XmlDocId.MemberId( null ) );
    }

    [Fact]
    public void MemberId_WithConstructor_ReturnsMethodPrefix()
    {
        var member = typeof( TestClass ).GetConstructor( [typeof( int )] );

        var id = XmlDocId.MemberId( member );

        Assert.StartsWith( "M:", id );
    }

    [Fact]
    public void MemberId_WithMethod_ReturnsMethodPrefix()
    {
        var member = typeof( TestClass ).GetMethod( nameof( TestClass.Add ) );

        var id = XmlDocId.MemberId( member );

        Assert.StartsWith( "M:", id );
    }

    [Fact]
    public void MemberId_WithProperty_ReturnsPropertyPrefix()
    {
        var member = typeof( TestClass ).GetProperty( nameof( TestClass.IntProperty ) );

        var id = XmlDocId.MemberId( member );

        Assert.StartsWith( "P:", id );
    }

    [Fact]
    public void MemberId_WithField_ReturnsPropertyPrefix()
    {
        var member = typeof( TestClass ).GetField( nameof( TestClass.IntField ) );

        var id = XmlDocId.MemberId( member );

        Assert.StartsWith( "F:", id );
    }

    [Fact]
    public void MemberId_WithNestedType_ReturnsTypePrefix()
    {
        var member = typeof( TestClass );

        var id = XmlDocId.MemberId( member );

        Assert.StartsWith( "T:", id );
    }

    [Fact]
    public void MemberId_WithEvent_ReturnsPropertyPrefix()
    {
        var member = typeof( TestClass ).GetEvent( nameof( TestClass.Event ) );

        var id = XmlDocId.MemberId( member );

        Assert.StartsWith( "E:", id );
    }

    [Fact]
    public void MemberId_WithInvalidMemberType_ThrowsException()
    {
        var mockMember = new Mock<MemberInfo>( MockBehavior.Strict );

        mockMember.Setup( m => m.MemberType ).Returns( MemberTypes.Custom );

        Assert.Throws<NotSupportedException>( () => XmlDocId.MemberId( mockMember.Object ) );
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Create a mock of a type without a namespace or name set.
    /// </summary>
    /// <returns>A mocking object.</returns>
    internal static Mock<Type> GetTypeMock()
    {
        var mock = new Mock<Type>( MockBehavior.Strict );

        mock.Setup( m => m.IsGenericType ).Returns( false );
        mock.Setup( m => m.IsGenericTypeDefinition ).Returns( false );
        mock.Setup( m => m.IsGenericParameter ).Returns( false );
        mock.Setup( m => m.ContainsGenericParameters ).Returns( false );
        mock.Setup( m => m.GetGenericArguments() ).Returns( [] );
        mock.Setup( m => m.MemberType ).Returns( MemberTypes.TypeInfo );
        mock.Setup( m => m.DeclaringType ).Returns( ( Type? ) null );

        return mock;
    }

    /// <summary>
    /// Create a mock of a type with the specified namespace and name.
    /// </summary>
    /// <param name="namespace">The namespace of the type.</param>
    /// <param name="name">The name of the type.</param>
    /// <returns>A mocking object.</returns>
    internal static Mock<Type> GetTypeMock( string? @namespace, string name )
    {
        var mock = GetTypeMock();

        mock.Setup( m => m.Name ).Returns( name );
        mock.Setup( m => m.Namespace ).Returns( @namespace );
        mock.Setup( m => m.FullName ).Returns( () =>
        {
            return @namespace != null
                ? $"{@namespace}.{name}"
                : name;
        } );

        return mock;
    }

    #endregion

    #region Support Classes

    class TestClass
    {
        public int IntField;

        public int IntProperty { get; set; }

        public event EventHandler? Event;

        public int Item
        {
            get => IntField;
            set => IntField = value;
        }

        public TestClass( int value )
        {
            IntField = value;
        }

        public static implicit operator int( TestClass t ) => t.IntField;

        public static explicit operator TestClass( int n ) => new( n );

        public int Add( int a, int b )
        {
            return a + b + IntField;
        }

        public void PrintGeneric<T>( T value )
        {
            _ = value?.ToString() + ToString();
        }

        public void ByRef( ref int value )
        {
            value = IntField;
        }

        public void Out( out int value )
        {
            value = IntField;
        }
    }

    enum TestEnum
    {
        Zero = 0
    }

    class TestClassItemWithGetter
    {
        public int this[int index] => 0;
    }

    class TestClassItemWithoutGetter
    {
        public int this[int index]
        {
            set { }
        }
    }

    #endregion
}
