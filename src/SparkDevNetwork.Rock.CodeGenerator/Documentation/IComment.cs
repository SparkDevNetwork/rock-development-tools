namespace SparkDevNetwork.Rock.CodeGenerator.Documentation
{
    /// <summary>
    /// Representation of a single comment.
    /// </summary>
    public interface IComment
    {
        /// <summary>
        /// Gets the text content.
        /// </summary>
        string Content { get; }

        /// <summary>
        /// Gets the plain text content.
        /// </summary>
        string PlainText { get; }
    }
}
