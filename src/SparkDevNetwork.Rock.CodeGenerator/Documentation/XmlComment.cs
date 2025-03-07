﻿using System;
using System.Linq;
using System.Text;
using System.Xml.XPath;

namespace SparkDevNetwork.Rock.CodeGenerator.Documentation
{
    /// <summary>
    /// Representation of a single comment.
    /// </summary>
    public class XmlComment : IComment
    {
        #region Properties

        /// <summary>
        /// Gets the text content.
        /// </summary>
        /// <value>The text content.</value>
        public string Content { get; }

        /// <summary>
        /// Gets the plain text content.
        /// </summary>
        /// <value>The plain text content.</value>
        public string PlainText { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlComment"/> class.
        /// </summary>
        /// <param name="node">The node that will be used to parse the comment information.</param>
        public XmlComment( XPathNavigator node )
        {
            Content = GetContent( node );
            PlainText = GetPlainText( node );
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the size of the indentation that should be trimmed from comments.
        /// </summary>
        /// <param name="node">The node that contains the comment.</param>
        /// <returns>The number of whitespace characters that should be trimmed from the start of all lines.</returns>
        internal static int GetIndentSize( XPathNavigator node )
        {
            var outerText = node.OuterXml;

            // Determine the indentation size of the text.
            var endMarkIndex = outerText.LastIndexOf( "</" );
            int indentSize = 0;
            while ( endMarkIndex > 0 && outerText[endMarkIndex - 1] == ' ' )
            {
                indentSize++;
                endMarkIndex--;
            }

            return indentSize;
        }

        /// <summary>
        /// Gets the text content for the node including any inner XML content.
        /// </summary>
        /// <param name="node">The node containing the comment.</param>
        /// <returns>A string that represents the inner content text.</returns>
        internal static string GetContent( XPathNavigator node )
        {
            var text = node.InnerXml;

            // Determine the indentation size of the text.
            int indentSize = GetIndentSize( node );

            // XmlDocument will normalize \r\n to \n on platforms
            // that only use \n so we need to split for both patterns.
            var textLines = text.Split( new string[] { "\r\n", "\n" }, StringSplitOptions.None )
                .Select( t => t.SubstringSafe( indentSize ) )
                .ToList();

            // Remove empty lines at start of content.
            while ( textLines.Any() && textLines.First().IsNullOrWhiteSpace() )
            {
                textLines = textLines.Skip( 1 ).ToList();
            }

            // Remove empty lines at end of content.
            while ( textLines.Any() && textLines.Last().IsNullOrWhiteSpace() )
            {
                textLines = textLines.Take( textLines.Count - 1 ).ToList();
            }

            return string.Join( " ", textLines );
        }

        /// <summary>
        /// Gets the plain text comment string for the node. 
        /// </summary>
        /// <param name="node">The node that contains the comment information.</param>
        /// <returns>A string that contains the plain text comment after removing any XML data.</returns>
        internal static string GetPlainText( XPathNavigator node )
        {
            var children = node.Select( "node()" );
            var sb = new StringBuilder();

            // Walk each child of the node and append the plain text
            // of any text nodes or child element nodes.
            foreach ( XPathNavigator child in children )
            {
                if ( child.NodeType == XPathNodeType.Text )
                {
                    sb.Append( child.OuterXml );
                }
                else if ( child.NodeType == XPathNodeType.Element )
                {
                    if ( !child.InnerXml.IsNullOrWhiteSpace() )
                    {
                        sb.Append( child.InnerXml );
                    }
                    else if ( !child.GetAttribute( "cref", string.Empty ).IsNullOrWhiteSpace() )
                    {
                        sb.Append( child.GetAttribute( "cref", string.Empty ).SubstringSafe( 2 ) );
                    }
                }
            }

            var text = sb.ToString();

            // Determine the indentation size of the text.
            int indentSize = GetIndentSize( node );

            // XmlDocument will normalize \r\n to \n on platforms
            // that only use \n so we need to split for both patterns.
            var textLines = text.Split( new string[] { "\r\n", "\n" }, StringSplitOptions.None )
                .Select( t => t.SubstringSafe( indentSize ) )
                .ToList();

            // Remove empty lines at start of content.
            while ( textLines.Any() && textLines.First().IsNullOrWhiteSpace() )
            {
                textLines = textLines.Skip( 1 ).ToList();
            }

            // Remove empty lines at end of content.
            while ( textLines.Any() && textLines.Last().IsNullOrWhiteSpace() )
            {
                textLines = textLines.Take( textLines.Count - 1 ).ToList();
            }

            return string.Join( "\r\n", textLines );
        }

        #endregion
    }
}
