using System;
using System.Collections.Generic;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SparkDevNetwork.Rock.Build.Tasks
{
    /// <summary>
    /// Gets the version number from the plugin file.
    /// </summary>
    public class GetPluginVersion : Task
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
        /// The detected version number from the plugin configuration file.
        /// </summary>
        [Output]
        public string Version { get; set; }

        /// <inheritdoc/>
        public override bool Execute()
        {
            // Check if the plugin file exists, it's fine if it doesn't as
            // we will just skip processing.
            if ( string.IsNullOrWhiteSpace( PluginConfigurationFile ) || !FileSystem.FileExists( PluginConfigurationFile ) )
            {
                Log.LogMessage( MessageImportance.Normal, $"The file '{PluginConfigurationFile}' does not exist." );
                Version = string.Empty;

                return true;
            }

            try
            {
                var json = FileSystem.ReadAllText( PluginConfigurationFile );
                var plugin = ( Dictionary<string, object> ) SimpleJson.DeserializeObject( json );

                if ( plugin.TryGetValue( "version", out var version ) )
                {
                    Version = version.ToString();
                    Log.LogMessage( MessageImportance.High, $"Setting version to '{Version}'." );
                }
                else
                {
                    Version = string.Empty;
                }

                return true;
            }
            catch ( Exception ex )
            {
                Log.LogError( $"An error occurred while trying to get the version number from the plugin file. {ex.Message}" );
                Version = string.Empty;

                return false;
            }
        }
    }
}
