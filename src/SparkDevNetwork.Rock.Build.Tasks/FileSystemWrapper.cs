using System.Diagnostics.CodeAnalysis;

namespace SparkDevNetwork.Rock.Build.Tasks
{
    /// <summary>
    /// Provides access to the real fiel system.
    /// </summary>
    [ExcludeFromCodeCoverage]
    class FileSystemWrapper : IFileSystem
    {
        /// <inheritdoc/>
        public void FileCopy( string sourceFileName, string destFileName, bool overwrite )
            => System.IO.File.Copy( sourceFileName, destFileName, overwrite );

        /// <inheritdoc/>
        public bool FileExists( string path )
            => System.IO.File.Exists( path );

        /// <inheritdoc/>
        public string GetFileName( string path )
            => System.IO.Path.GetFileName( path );

        /// <inheritdoc/>
        public string GetFullPath( string path )
            => System.IO.Path.GetFullPath( path );

        /// <inheritdoc/>
        public string PathCombine( string path1, string path2 )
            => System.IO.Path.Combine( path1, path2 );

        /// <inheritdoc/>
        public string ReadAllText( string path )
            => System.IO.File.ReadAllText( path );
    }
}
