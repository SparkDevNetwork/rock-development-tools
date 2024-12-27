using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SparkDevNetwork.Rock.CodeGenerator.Documentation
{
    /// <summary>
    /// XML documentation parser and reader. This is extremely limited but it
    /// serves our needs, which is just passing around the summary and value
    /// comments on types and members.
    /// </summary>
    /// <remarks>
    /// This is essentially a stripped down version of https://github.com/loxsmoke/DocXml
    /// but performs much faster because it caches all the data since the XPath
    /// navigation system is so slow.
    /// </remarks>
    public class XmlDocReader : IDocumentationProvider
    {
        #region Fields

        /// <summary>
        /// The comments that have been loaded and parsed from files.
        /// </summary>
        private IReadOnlyDictionary<string, XmlCommentSet> _comments = new Dictionary<string, XmlCommentSet>();

        #endregion

        #region Methods

        /// <summary>
        /// Reads the comments from the file specified.
        /// </summary>
        /// <param name="filename">The path and name of the file to read.</param>
        public void ReadCommentsFrom( string filename )
        {
            var document = XDocument.Load( filename, LoadOptions.PreserveWhitespace );
            var navigator = document.CreateNavigator();
            var commentsTable = new Dictionary<string, XmlCommentSet>( ( IDictionary<string, XmlCommentSet> ) _comments );

            // Find all documentation member nodes.
            var memberNodes = navigator.Select( "/doc/members/member" );

            foreach ( XPathNavigator node in memberNodes )
            {
                var memberName = node.GetAttribute( "name", string.Empty );

                // Read any inheritdoc node and store the reference for later.
                var inheritDoc = node.SelectSingleNode( "inheritdoc" );

                // Read the comments from the node.
                var comments = new XmlCommentSet(
                    GetDocComment( node.SelectSingleNode( "summary" ) ),
                    GetDocComment( node.SelectSingleNode( "value" ) ),
                    inheritDoc?.GetAttribute( "cref", string.Empty ) );

                if ( !commentsTable.ContainsKey( memberName ) )
                {
                    commentsTable.Add( memberName, comments );
                }
            }

            _comments = commentsTable;
        }

        /// <summary>
        /// Creates a new <see cref="Comment"/> from the given node. This is
        /// a single node like "summary" or "value".
        /// </summary>
        /// <param name="node">The node to read that contains the comment text.</param>
        /// <returns>A new <see cref="Comment"/> object that represents the comment text or <c>null</c> if the node was not valid.</returns>
        private XmlComment GetDocComment( XPathNavigator node )
        {
            if ( node == null )
            {
                return null;
            }

            return new XmlComment( node );
        }

        /// <summary>
        /// Resolves the inheritdoc comments for the given type.
        /// </summary>
        /// <param name="comments">The comments that were read from the type.</param>
        /// <param name="type">The type used for determining inheritence.</param>
        /// <returns>An instance of <see cref="XmlCommentSet"/> that contains the resolved comments for this type.</returns>
        private XmlCommentSet ResolveTypeComments( XmlCommentSet comments, Type type )
        {
            if ( comments.InheritFrom == null )
            {
                return comments;
            }

            // Resolve comments from specific cref attribute.
            if ( comments.InheritFrom != string.Empty )
            {
                return MergeComments( comments, GetComments( comments.InheritFrom ) );
            }

            // Resolve comments from inheritence.
            if ( type.BaseType != null && type.BaseType != typeof( object ) )
            {
                return MergeComments( comments, GetTypeXmlComments( type.BaseType ) );
            }

            return comments;
        }

        /// <summary>
        /// Resolves the inheritdoc comments for the given member.
        /// </summary>
        /// <param name="comments">The comments that were read from the member.</param>
        /// <param name="memberInfo">The member used to track inheritence.</param>
        /// <returns>An instance of <see cref="XmlCommentSet" /> that contains the resolved comments for this type.</returns>
        private XmlCommentSet ResolveMemberComments( XmlCommentSet comments, MemberInfo memberInfo )
        {
            if ( comments.InheritFrom == null )
            {
                return comments;
            }

            // Resolve comments from specific cref attribute.
            if ( comments.InheritFrom != string.Empty )
            {
                return MergeComments( comments, GetComments( comments.InheritFrom ) );
            }

            // TODO: I think we need to check for interfaces or base virtual methods.

            return comments;
        }

        /// <summary>
        /// Merges the comments from <paramref name="source"/> and <paramref name="additional"/>
        /// together and returns a new <see cref="XmlCommentSet"/> instance.
        /// </summary>
        /// <param name="source">The source comments that take priority.</param>
        /// <param name="additional">The additional comments that should be added.</param>
        /// <returns>A new instance of <see cref="XmlCommentSet"/> with the merged information.</returns>
        private XmlCommentSet MergeComments( XmlCommentSet source, XmlCommentSet additional )
        {
            return new XmlCommentSet(
                source.Summary ?? additional.Summary,
                null,
                source.InheritFrom );
        }

        /// <summary>
        /// Get the <see cref="XmlCommentSet"/> object for the given cref name.
        /// </summary>
        /// <param name="cref">The XMLDoc unique identifier for the member.</param>
        /// <returns>An instance of <see cref="XmlCommentSet"/> or <c>null</c> if the member was not found.</returns>
        private XmlCommentSet GetComments( string cref )
        {
            if ( !_comments.TryGetValue( cref, out var comments ) )
            {
                return null;
            }

            return comments;
        }

        /// <summary>
        /// Gets the comments associated with the given type.
        /// </summary>
        /// <param name="type">The type whose comments should be searched for.</param>
        /// <returns>An instance of <see cref="XmlCommentSet"/> that represents the documentation comments or <c>null</c> if not found.</returns>
        private XmlCommentSet GetTypeXmlComments( Type type )
        {
            if ( !_comments.TryGetValue( type.TypeId(), out var comments ) )
            {
                return null;
            }

            return ResolveTypeComments( comments, type );
        }

        /// <summary>
        /// Gets the comments associated with the given type.
        /// </summary>
        /// <param name="type">The type whose comments should be searched for.</param>
        /// <returns>An instance of <see cref="ICommentSet"/> that represents the documentation comments or <c>null</c> if not found.</returns>
        public ICommentSet GetTypeComments( Type type )
        {
            return GetTypeXmlComments( type );
        }

        /// <summary>
        /// Gets the comments associated with the given member.
        /// </summary>
        /// <param name="memberInfo">The member whose comments should be searched for.</param>
        /// <returns>An instance of <see cref="ICommentSet"/> that represents the documentation comments or <c>null</c> if not found.</returns>
        public ICommentSet GetMemberComments( MemberInfo memberInfo )
        {
            if ( !_comments.TryGetValue( memberInfo.MemberId(), out var comments ) )
            {
                return null;
            }

            return ResolveMemberComments( comments, memberInfo );
        }

        #endregion
    }
}
