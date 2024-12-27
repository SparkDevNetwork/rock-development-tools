namespace SparkDevNetwork.Rock.CodeGenerator.Documentation
{
    /// <summary>
    /// The comments that were loaded for a given type or member.
    /// </summary>
    public class XmlCommentSet : ICommentSet
    {
        /// <inheritdoc/>
        public IComment Summary { get; }

        /// <inheritdoc/>
        public IComment Value { get; }

        /// <summary>
        /// The inherit from cref value.
        /// </summary>
        public string InheritFrom { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlCommentSet"/> class.
        /// </summary>
        /// <param name="summary">The summary comment content.</param>
        /// <param name="value">The value comment content or <c>null</c>.</param>
        /// <param name="inheritFrom">The inherit from cref value or <c>null</c>.</param>
        public XmlCommentSet( IComment summary, IComment value, string inheritFrom )
        {
            Summary = summary;
            Value = value;
            InheritFrom = inheritFrom;
        }
    }
}
