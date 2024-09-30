namespace SparkDevNetwork.Rock.CodeGenerator.Documentation
{
    /// <summary>
    /// The comments that were loaded for a given type or member.
    /// </summary>
    public class XmlCommentSet : ICommentSet
    {
        /// <inheritdoc/>
        public IComment Summary { get; set; }

        /// <inheritdoc/>
        public IComment Value { get; set; }

        /// <summary>
        /// Gets or sets the inherit from cref value.
        /// </summary>
        public string InheritFrom { get; set; }
    }
}
