using System.Collections;

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
