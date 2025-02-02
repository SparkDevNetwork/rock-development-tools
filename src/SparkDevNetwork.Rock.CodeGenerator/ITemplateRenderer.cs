using System.Collections.Generic;

namespace SparkDevNetwork.Rock.CodeGenerator
{
    /// <summary>
    /// Provides functionality to render a template into a final source file.
    /// </summary>
    public interface ITemplateRenderer
    {
        /// <summary>
        /// Resolves the template into a final rendered output.
        /// </summary>
        /// <param name="template">The template to be rendered.</param>
        /// <param name="mergeFields">The objects to make available in the template.</param>
        /// <returns>The rendered output from the template.</returns>
        string Render( string template, Dictionary<string, object> mergeFields );
    }
}
