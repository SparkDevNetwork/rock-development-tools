namespace SparkDevNetwork.Rock.CodeGenerator.Documentation
{
    /// <summary>
    /// The comments that were loaded for a given type or member.
    /// </summary>
    public interface ICommentSet
    {
        /// <summary>
        /// The summary comment content.
        /// </summary>
        IComment Summary { get; }

        /// <summary>
        /// The value comment content.
        /// </summary>
        IComment Value { get; }
    }
}
