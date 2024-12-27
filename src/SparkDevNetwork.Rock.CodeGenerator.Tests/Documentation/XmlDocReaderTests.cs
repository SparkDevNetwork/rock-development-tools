using System.Reflection;

using Moq;

using SparkDevNetwork.Rock.CodeGenerator.Documentation;

namespace SparkDevNetwork.Rock.CodeGenerator.Tests.Documentation;

public class XmlDocReaderTests
{
    private const string SampleXmlDoc = """
        <?xml version="1.0"?>
        <doc>
            <members>
                <member name="T:TestLib.State">
                    <summary>
                    The type of state.
                    </summary>
                </member>
                <member name="P:TestLib.State.PropValue">
                    <summary>
                    The value of the state.
                    </summary>
                    <value>
                    An integer.
                    </value>
                </member>
                <member name="P:TestLib.State.ExplicitInteritdocPropValue">
                    <inheritdoc cref="P:TestLib.State.PropValue" />
                </member>
                <member name="T:TestLib.ExplicitInheritdocState">
                    <inheritdoc cref="T:TestLib.State"/>
                </member>
                <member name="T:TestLib.ImplicitInheritdocState">
                    <inheritdoc />
                </member>
                <member name="P:TestLib.ImplicitInheritdocState.PropValue">
                    <inheritdoc />
                </member>
                <member name="T:TestLib.MissingInheritdocState">
                    <inheritdoc cref="T:TestLib.ThisDoesNotExist" />
                </member>
            </members>
        </doc>
        """;

    [Fact]
    public void ReadCommentsFrom_WithSampleXml_ReadsAllMembers()
    {
        var reader = new XmlDocReader();
        var document = new System.Xml.XmlDocument();
        document.LoadXml( SampleXmlDoc );

        reader.ReadCommentsFrom( document.CreateNavigator() );

        Assert.Equal( 7, reader.GetAllXmlTypesIds().Count() );
    }

    #region GetTypeComments

    [Fact]
    public void GetTypeComments_WithKnownType_ReturnsSummary()
    {
        var reader = new XmlDocReader();
        var document = new System.Xml.XmlDocument();
        document.LoadXml( SampleXmlDoc );

        reader.ReadCommentsFrom( document.CreateNavigator() );

        var typeMock = XmlDocIdTests.GetTypeMock( "TestLib", "State" );

        var comments = reader.GetTypeComments( typeMock.Object );

        Assert.NotNull( comments );
        Assert.NotNull( comments.Summary );
        Assert.Equal( "The type of state.", comments.Summary.PlainText );
    }

    [Fact]
    public void GetTypeComments_WithKnownType_ReturnsExplicitInheritdocSummary()
    {
        var reader = new XmlDocReader();
        var document = new System.Xml.XmlDocument();
        document.LoadXml( SampleXmlDoc );

        reader.ReadCommentsFrom( document.CreateNavigator() );

        var typeMock = XmlDocIdTests.GetTypeMock( "TestLib", "ExplicitInheritdocState" );
        var comments = reader.GetTypeComments( typeMock.Object );

        Assert.NotNull( comments );
        Assert.NotNull( comments.Summary );
        Assert.Equal( "The type of state.", comments.Summary.PlainText );
    }

    [Fact]
    public void GetTypeComments_WithKnownType_ReturnsImplicitInheritdocSummary()
    {
        var reader = new XmlDocReader();
        var document = new System.Xml.XmlDocument();
        document.LoadXml( SampleXmlDoc );

        reader.ReadCommentsFrom( document.CreateNavigator() );

        var baseTypeMock = XmlDocIdTests.GetTypeMock( "TestLib", "State" );
        var typeMock = XmlDocIdTests.GetTypeMock( "TestLib", "ImplicitInheritdocState" );

        typeMock.Setup( m => m.BaseType ).Returns( baseTypeMock.Object );

        var comments = reader.GetTypeComments( typeMock.Object );

        Assert.NotNull( comments );
        Assert.NotNull( comments.Summary );
        Assert.Equal( "The type of state.", comments.Summary.PlainText );
    }

    [Fact]
    public void GetTypeComments_WithNullBaseType_ReturnsEmptyComments()
    {
        var reader = new XmlDocReader();
        var document = new System.Xml.XmlDocument();
        document.LoadXml( SampleXmlDoc );

        reader.ReadCommentsFrom( document.CreateNavigator() );

        var typeMock = XmlDocIdTests.GetTypeMock( "TestLib", "ImplicitInheritdocState" );

        typeMock.Setup( m => m.BaseType ).Returns( ( Type? ) null );

        var comments = reader.GetTypeComments( typeMock.Object );

        Assert.NotNull( comments );
        Assert.Null( comments.Summary );
        Assert.Null( comments.Value );
    }

    [Fact]
    public void GetTypeComments_WithUnknownType_ReturnsNull()
    {
        var reader = new XmlDocReader();
        var document = new System.Xml.XmlDocument();
        document.LoadXml( SampleXmlDoc );

        reader.ReadCommentsFrom( document.CreateNavigator() );

        var typeMock = XmlDocIdTests.GetTypeMock( "TestLib", "MissingType" );

        var comments = reader.GetTypeComments( typeMock.Object );

        Assert.Null( comments );
    }

    [Fact]
    public void GetTypeComments_WithTypeThatHasInvalidInheritdoc_ReturnsEmptySummary()
    {
        var reader = new XmlDocReader();
        var document = new System.Xml.XmlDocument();
        document.LoadXml( SampleXmlDoc );

        reader.ReadCommentsFrom( document.CreateNavigator() );

        var typeMock = XmlDocIdTests.GetTypeMock( "TestLib", "MissingInheritdocState" );

        var comments = reader.GetTypeComments( typeMock.Object );

        Assert.NotNull( comments );
        Assert.Null( comments.Summary );
        Assert.Null( comments.Value );
    }

    #endregion

    #region GetMemberComments

    [Fact]
    public void GetMemberComments_WithMissingMember_ReturnsNull()
    {
        var reader = new XmlDocReader();
        var document = new System.Xml.XmlDocument();
        document.LoadXml( SampleXmlDoc );

        reader.ReadCommentsFrom( document.CreateNavigator() );

        var typeMock = XmlDocIdTests.GetTypeMock( "TestLib", "State" );
        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );

        propertyMock.Setup( m => m.MemberType ).Returns( MemberTypes.Property );
        propertyMock.Setup( m => m.Name ).Returns( "InvalidProperty" );
        propertyMock.Setup( m => m.DeclaringType ).Returns( typeMock.Object );

        var comments = reader.GetMemberComments( propertyMock.Object );

        Assert.Null( comments );
    }

    [Fact]
    public void GetMemberComments_WithDocumentedProperty_ReturnsSummary()
    {
        var reader = new XmlDocReader();
        var document = new System.Xml.XmlDocument();
        document.LoadXml( SampleXmlDoc );

        reader.ReadCommentsFrom( document.CreateNavigator() );

        var typeMock = XmlDocIdTests.GetTypeMock( "TestLib", "State" );
        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );

        propertyMock.Setup( m => m.MemberType ).Returns( MemberTypes.Property );
        propertyMock.Setup( m => m.Name ).Returns( "PropValue" );
        propertyMock.Setup( m => m.DeclaringType ).Returns( typeMock.Object );

        var comments = reader.GetMemberComments( propertyMock.Object );

        Assert.NotNull( comments );
        Assert.NotNull( comments.Summary );
        Assert.Equal( "The value of the state.", comments.Summary.PlainText );
    }

    [Fact]
    public void GetMemberComments_WithDocumentedProperty_ReturnsValue()
    {
        var reader = new XmlDocReader();
        var document = new System.Xml.XmlDocument();
        document.LoadXml( SampleXmlDoc );

        reader.ReadCommentsFrom( document.CreateNavigator() );

        var typeMock = XmlDocIdTests.GetTypeMock( "TestLib", "State" );
        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );

        propertyMock.Setup( m => m.MemberType ).Returns( MemberTypes.Property );
        propertyMock.Setup( m => m.Name ).Returns( "PropValue" );
        propertyMock.Setup( m => m.DeclaringType ).Returns( typeMock.Object );

        var comments = reader.GetMemberComments( propertyMock.Object );

        Assert.NotNull( comments );
        Assert.NotNull( comments.Value );
        Assert.Equal( "An integer.", comments.Value.PlainText );
    }

    [Fact]
    public void GetMemberComments_WithProperty_ReturnsExplicitInheritdocSummary()
    {
        var reader = new XmlDocReader();
        var document = new System.Xml.XmlDocument();
        document.LoadXml( SampleXmlDoc );

        reader.ReadCommentsFrom( document.CreateNavigator() );

        var typeMock = XmlDocIdTests.GetTypeMock( "TestLib", "State" );
        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );

        propertyMock.Setup( m => m.MemberType ).Returns( MemberTypes.Property );
        propertyMock.Setup( m => m.Name ).Returns( "ExplicitInteritdocPropValue" );
        propertyMock.Setup( m => m.DeclaringType ).Returns( typeMock.Object );

        var comments = reader.GetMemberComments( propertyMock.Object );

        Assert.NotNull( comments );
        Assert.NotNull( comments.Summary );
        Assert.Equal( "The value of the state.", comments.Summary.PlainText );
    }

    [Fact]
    public void GetMemberComments_WithImplicitInheritdocProperty_ReturnsEmptySummary()
    {
        var reader = new XmlDocReader();
        var document = new System.Xml.XmlDocument();
        document.LoadXml( SampleXmlDoc );

        reader.ReadCommentsFrom( document.CreateNavigator() );

        var typeMock = XmlDocIdTests.GetTypeMock( "TestLib", "ImplicitInheritdocState" );
        var propertyMock = new Mock<PropertyInfo>( MockBehavior.Strict );

        propertyMock.Setup( m => m.MemberType ).Returns( MemberTypes.Property );
        propertyMock.Setup( m => m.Name ).Returns( "PropValue" );
        propertyMock.Setup( m => m.DeclaringType ).Returns( typeMock.Object );

        var comments = reader.GetMemberComments( propertyMock.Object );

        // Note: This is a limitation of the current implementation. If the
        // implementation is updated to support searching base types and interfaces
        // then this test should be updated to reflect that.
        Assert.NotNull( comments );
        Assert.Null( comments.Summary );
    }

    #endregion
}
