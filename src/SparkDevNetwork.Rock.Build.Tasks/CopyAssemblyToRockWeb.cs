using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SparkDevNetwork.Rock.Build.Tasks
{
    /// <summary>
    /// Copies the specified assembly to the RockWeb bin folder. This will
    /// also copy any dependencies unless they are Rock dependencies assemblies.
    /// </summary>
    public class CopyAssemblyToRockWeb : Task
    {
        /// <summary>
        /// The primary DLL to be copied. All references not also referenced
        /// by Rock.dll will also be copied.
        /// </summary>
        [Required]
        public string Source { get; set; }

        /// <summary>
        /// The destination directory to copy the files into.
        /// </summary>
        [Required]
        public string Destination { get; set; }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var sourceDirectory = Path.GetDirectoryName( Source );

            Log.LogMessage( MessageImportance.High, "Copying files to RockWeb." );

            var rockAssemblies = GetAllReferences( Path.Combine( sourceDirectory, "Rock.dll" ), true );
            var primaryReferences = GetAllReferences( Source, true );

            foreach ( var asmName in primaryReferences )
            {
                if ( rockAssemblies.Contains( asmName ) )
                {
                    continue;
                }

                var dllFile = Path.Combine( sourceDirectory, $"{asmName}.dll" );
                var pdbFile = Path.Combine( sourceDirectory, $"{asmName}.pdb" );

                if ( !File.Exists( dllFile ) )
                {
                    continue;
                }

                CopyFile( dllFile );

                if ( File.Exists( pdbFile ) )
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
            var destFile = Path.Combine( Path.GetFullPath( Destination ), Path.GetFileName( sourceFile ) );

            File.Copy( sourceFile, destFile, true );

            Log.LogMessage( MessageImportance.High, $"  {sourceFile} => {destFile}" );
        }

        /// <summary>
        /// Gets all references recursively for the DLL.
        /// </summary>
        /// <param name="filename">The primary DLL to be checked.</param>
        /// <param name="includePrimary">If <c>true</c> then the primary DLL will be included in results.</param>
        /// <returns>A list of assembly names.</returns>
        private static IEnumerable<string> GetAllReferences( string filename, bool includePrimary )
        {
            var runtimeAssemblies = Directory.GetFiles( RuntimeEnvironment.GetRuntimeDirectory(), "*.dll" );
            var resolver = new PathAssemblyResolver( runtimeAssemblies );
            var mlc = new MetadataLoadContext( resolver );

            var references = new List<string>();
            var stack = new List<string>( new[] { filename } );

            if ( includePrimary )
            {
                references.Add( Path.GetFileNameWithoutExtension( filename ) );
            }

            while ( stack.Count > 0 )
            {
                var name = stack[0];

                stack.RemoveAt( 0 );

                if ( !File.Exists( name ) )
                {
                    continue;
                }

                var asm = mlc.LoadFromAssemblyPath( name );
                var refs = asm.GetReferencedAssemblies().Select( r => r.Name );

                foreach ( var reference in refs )
                {
                    if ( !references.Contains( reference ) )
                    {
                        references.Add( reference );
                        stack.Add( Path.Combine( Path.GetDirectoryName( filename ), $"{reference}.dll" ) );
                    }
                }
            }

            return references;
        }
    }
}
