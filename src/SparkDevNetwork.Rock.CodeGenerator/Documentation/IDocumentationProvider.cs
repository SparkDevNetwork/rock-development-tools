using System;
using System.Reflection;

namespace SparkDevNetwork.Rock.CodeGenerator.Documentation
{
    /// <summary>
    /// Provides documentation for various types when generating code.
    /// </summary>
    public interface IDocumentationProvider
    {
        /// <summary>
        /// Gets the comments associated with the given type.
        /// </summary>
        /// <param name="type">The type whose comments should be searched for.</param>
        /// <returns>An instance of <see cref="ICommentSet"/> that represents the documentation comments or <c>null</c> if not found.</returns>
        ICommentSet GetTypeComments( Type type );

        /// <summary>
        /// Gets the comments associated with the given member.
        /// </summary>
        /// <param name="memberInfo">The member whose comments should be searched for.</param>
        /// <returns>An instance of <see cref="ICommentSet"/> that represents the documentation comments or <c>null</c> if not found.</returns>
        ICommentSet GetMemberComments( MemberInfo memberInfo );
    }
}
