using System.Diagnostics.CodeAnalysis;

namespace SparkDevNetwork.Rock.Build.Tasks
{
    /// <summary>
    /// Provides access to the real fiel system.
    /// </summary>
    [ExcludeFromCodeCoverage]
    class DefaultFileSystem : IFileSystem
    {
        /// <inheritdoc/>
        public bool FileExists( string path ) => System.IO.File.Exists( path );

        /// <inheritdoc/>
        public string ReadAllText( string path ) => System.IO.File.ReadAllText( path );
    }
}
