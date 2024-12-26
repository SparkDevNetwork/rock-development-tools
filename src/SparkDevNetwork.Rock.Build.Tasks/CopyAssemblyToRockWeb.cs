using System;
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SparkDevNetwork.Rock.Build.Tasks
{
    /// <summary>
    /// Copies the specified assembly to the RockWeb bin folder.
    /// </summary>
    public class CopyAssemblyToRockWeb : Task
    {
        /// <summary>
        /// The object to use when interacting with the filesystem. Used to
        /// support unit testing.
        /// </summary>
        internal IFileSystem FileSystem { get; set; } = new FileSystemWrapper();

        /// <summary>
        /// The source directory that the files are in.
        /// </summary>
        [Required]
        public string Source { get; set; }

        /// <summary>
        /// The files to copy from the <see cref="Source"/> directory. Multiple
        /// entries should be separated by semi-colon.
        /// </summary>
        [Required]
        public string Files { get; set; }

        /// <summary>
        /// The destination directory to copy the files into.
        /// </summary>
        [Required]
        public string Destination { get; set; }

        /// <inheritdoc/>
        public override bool Execute()
        {
            if ( string.IsNullOrWhiteSpace( Source ) )
            {
                Log.LogError( "The Source property must be set." );
                return false;
            }

            if ( string.IsNullOrWhiteSpace( Destination ) )
            {
                Log.LogError( "The Destination property must be set." );
                return false;
            }

            var sourceDirectory = Source;
            var files = Files.Split( new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries )
                .Select( f => f.Trim() )
                .ToList();

            Log.LogMessage( MessageImportance.High, "Copying files to RockWeb." );

            foreach ( var file in files )
            {
                var asmName = file;

                if ( asmName.EndsWith( ".dll", StringComparison.OrdinalIgnoreCase ) )
                {
                    // Remove the last four characters.
                    asmName = asmName.Substring( 0, asmName.Length - 4 );
                }

                var dllFile = FileSystem.PathCombine( sourceDirectory, $"{asmName}.dll" );
                var pdbFile = FileSystem.PathCombine( sourceDirectory, $"{asmName}.pdb" );

                if ( !FileSystem.FileExists( dllFile ) )
                {
                    Log.LogError( $"The file {dllFile} does not exist." );
                    return false;
                }

                CopyFile( dllFile );

                if ( FileSystem.FileExists( pdbFile ) )
                {
                    CopyFile( pdbFile );
                }
            }

            return true;
        }

        /// <summary>
        /// Copy the file to the destination.
        /// </summary>
        /// <param name="sourceFile">The file to be copied.</param>
        private void CopyFile( string sourceFile )
        {
            var destFile = FileSystem.PathCombine( FileSystem.GetFullPath( Destination ), FileSystem.GetFileName( sourceFile ) );

            FileSystem.FileCopy( sourceFile, destFile, true );

            Log.LogMessage( MessageImportance.High, $"  {sourceFile} => {destFile}" );
        }

        // /// <summary>
        // /// Gets all references recursively for the DLL.
        // /// </summary>
        // /// <remarks>
        // /// This method is not currently used but being left as it might be
        // /// used in the future.
        // /// </remarks>
        // /// <param name="filename">The primary DLL to be checked.</param>
        // /// <param name="includePrimary">If <c>true</c> then the primary DLL will be included in results.</param>
        // /// <returns>A list of assembly names.</returns>
        // private static IEnumerable<string> GetAllReferences( string filename, bool includePrimary )
        // {
        //     var runtimeAssemblies = Directory.GetFiles( RuntimeEnvironment.GetRuntimeDirectory(), "*.dll" );
        //     var resolver = new PathAssemblyResolver( runtimeAssemblies );
        //     var mlc = new MetadataLoadContext( resolver );

        //     var references = new List<string>();
        //     var stack = new List<string>( new[] { filename } );

        //     if ( includePrimary )
        //     {
        //         references.Add( Path.GetFileNameWithoutExtension( filename ) );
        //     }

        //     while ( stack.Count > 0 )
        //     {
        //         var name = stack[0];

        //         stack.RemoveAt( 0 );

        //         if ( !File.Exists( name ) )
        //         {
        //             continue;
        //         }

        //         var asm = mlc.LoadFromAssemblyPath( name );
        //         var refs = asm.GetReferencedAssemblies().Select( r => r.Name );

        //         foreach ( var reference in refs )
        //         {
        //             if ( !references.Contains( reference ) )
        //             {
        //                 references.Add( reference );
        //                 stack.Add( Path.Combine( Path.GetDirectoryName( filename ), $"{reference}.dll" ) );
        //             }
        //         }
        //     }

        //     return references;
        // }
    }
}
