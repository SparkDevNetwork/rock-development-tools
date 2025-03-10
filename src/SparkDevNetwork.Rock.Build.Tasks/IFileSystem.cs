namespace SparkDevNetwork.Rock.Build.Tasks
{
    /// <summary>
    /// Mockable file system interface for the tasks to use.
    /// </summary>
    public interface IFileSystem
    {
        /// <inheritdoc cref="System.IO.File.Exists(string?)"/>
        bool FileExists( string path );

        /// <inheritdoc cref="System.IO.File.ReadAllText(string)"/>
        string ReadAllText( string path );

        /// <inheritdoc cref="System.IO.Path.Combine(string, string)"/>
        string PathCombine( string path1, string path2 );

        /// <inheritdoc cref="System.IO.Path.GetFullPath(string)"/>
        string GetFullPath( string path );

        /// <inheritdoc cref="System.IO.Path.GetFileName(string)"/>
        string GetFileName( string path );

        /// <inheritdoc cref="System.IO.Path.GetDirectoryName(string)"/>
        string GetDirectoryName( string path );

        /// <inheritdoc cref="System.IO.File.Copy(string, string, bool)"/>
        void FileCopy( string sourceFileName, string destFileName, bool overwrite );
    }
}
