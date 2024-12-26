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
    }
}
