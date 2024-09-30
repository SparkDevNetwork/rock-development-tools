namespace SparkDevNetwork.Rock.CodeGenerator.Documentation
{
    /// <summary>
    /// The comments that were loaded for a given type or member.
    /// </summary>
    public interface ICommentSet
    {
        /// <summary>
        /// Gets or sets the summary comment content.
        /// </summary>
        /// <value>The summary comment content.</value>
        IComment Summary { get; set; }

        /// <summary>
        /// Gets or sets the value comment content.
        /// </summary>
        /// <value>The value comment content.</value>
        IComment Value { get; set; }
    }
}
