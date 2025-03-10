using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SparkDevNetwork.Rock.Build.Tasks
{
    /// <summary>
    /// Gets the path to the plugin environment from the plugin file.
    /// </summary>
    public class GetPluginEnvironmentDirectory : Task
    {
        /// <summary>
        /// The object to use when interacting with the filesystem. Used to
        /// support unit testing.
        /// </summary>
        internal IFileSystem FileSystem { get; set; } = new FileSystemWrapper();

        /// <summary>
        /// The file path to use when loading the plugin configuration file.
        /// </summary>
        [Required]
        public string PluginConfigurationFile { get; set; }

        /// <summary>
        /// The detected path of the environment or an empty string if not found.
        /// </summary>
        [Output]
        public string Path { get; set; }

        /// <inheritdoc/>
        public override bool Execute()
        {
            // Check if the plugin file exists, it's fine if it doesn't as
            // we will just skip processing.
            if ( string.IsNullOrWhiteSpace( PluginConfigurationFile ) || !FileSystem.FileExists( PluginConfigurationFile ) )
            {
                Log.LogMessage( MessageImportance.Normal, $"The file '{PluginConfigurationFile}' does not exist." );
                Path = string.Empty;

                return true;
            }

            var path = FileSystem.GetFullPath( PluginConfigurationFile );
            path = FileSystem.GetDirectoryName( path );

            while ( path != null && path.Length > 0 )
            {
                if ( FileSystem.FileExists( FileSystem.PathCombine( path, "environment.json" ) ) )
                {
                    Log.LogMessage( MessageImportance.High, $"Found environment.json in '{path}'." );
                    Path = path;

                    return true;
                }

                path = FileSystem.GetDirectoryName( path );
            }

            Log.LogMessage( MessageImportance.High, $"The environment.json file was not found, assuming no environment." );
            Path = string.Empty;
            return true;
        }
    }
}
