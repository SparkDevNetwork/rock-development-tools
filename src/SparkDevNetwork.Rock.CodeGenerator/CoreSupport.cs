namespace SparkDevNetwork.Rock.CodeGenerator
{
    /// <summary>
    /// Provides special support methods for working with core data from Rock.
    /// </summary>
    public static class CoreSupport
    {
        /// <summary>
        /// Gets the file system folder name (and C# namespace name) for the
        /// specified rock domain. If the string is not all caps then it will
        /// be returned as is. If it is all caps then special rules will be
        /// applied to format it correctly.
        /// </summary>
        /// <param name="domain">The Rock domain, such as CRM or Group.</param>
        /// <returns>The name in a format that can be used for file system folders and C# namespaces.</returns>
        public static string GetDomainFolderName( string domain )
        {
            // If the domain isn't all caps, then its already in the correct format.
            if ( domain != domain.ToUpper() )
            {
                return domain;
            }

            // 2 letter acronyms (such as UI) are kept as-is.
            if ( domain.Length == 2 )
            {
                return domain;
            }

            return domain.ToUpper()[0] + domain.Substring( 1 ).ToLower();
        }
    }
}
