using SparkDevNetwork.Rock.CodeGenerator;

namespace SparkDevNetwork.Rock.DevTool.Generators;

/// <summary>
/// Handles plugin code generation for TypeScript files.
/// </summary>
class PluginTypeScriptGenerator : TypeScriptViewModelGenerator
{
    private readonly string[] _targetPathComponents;

    public PluginTypeScriptGenerator( string[] targetPathComponents )
    {
        _targetPathComponents = targetPathComponents;
    }

    /// <inheritdoc/>
    protected override TypeScriptTypeDefinition? GetTypeScriptTypeDefinition( Type type, bool isRequired )
    {
        if ( string.IsNullOrWhiteSpace( type.Namespace ) )
        {
            return base.GetTypeScriptTypeDefinition( type, isRequired );
        }

        var namespaceComponents = type.Namespace.Split( '.' ).ToList();

        if ( type.IsClass && !type.IsNested && namespaceComponents.Contains( "ViewModels" ) && ( type.Name.EndsWith( "Bag" ) || type.Name.EndsWith( "Box" ) ) )
        {
            namespaceComponents = namespaceComponents.SkipWhile( c => c != "ViewModels" ).ToList();

            var pathComponents = GetPathReferenceComponents( [.. _targetPathComponents], namespaceComponents );
            var path = $"{string.Join( "/", pathComponents )}/{type.Name.ToCamelCase()}";

            var imports = new List<TypeScriptImport>
            {
                new TypeScriptImport
                {
                    SourcePath = $"{path}",
                    NamedImport = type.Name
                }
            };

            return new TypeScriptTypeDefinition( type.Name, imports );
        }
        else if ( namespaceComponents.Contains( "Enums" ) && type.IsEnum )
        {
            namespaceComponents = namespaceComponents.SkipWhile( c => c != "Enums" ).ToList();

            var pathComponents = GetPathReferenceComponents( [.. _targetPathComponents], namespaceComponents );
            var path = $"{string.Join( "/", pathComponents )}/{type.Name.ToCamelCase()}.partial";

            var imports = new List<TypeScriptImport>
                {
                    new TypeScriptImport
                    {
                        SourcePath = $"{path}",
                        NamedImport = type.Name
                    }
                };

            return new TypeScriptTypeDefinition( type.Name, imports );
        }

        return base.GetTypeScriptTypeDefinition( type, isRequired );
    }

    /// <summary>
    /// Get the components that make up a path reference from
    /// <paramref name="sourcePathComponents"/> to <paramref name="targetPathComponents"/>.
    /// </summary>
    /// <param name="sourcePathComponents">The components from the output directory to the directory of the source file.</param>
    /// <param name="targetPathComponents">The components from the output directory to the directory of the target file.</param>
    /// <returns>A list of path components.</returns>
    private static List<string> GetPathReferenceComponents( List<string> sourcePathComponents, List<string> targetPathComponents )
    {
        // The purpose of this is to remove common path components. Meaning
        // if we have a source path of "ViewModels/Dir1/DirA" and a target
        // of "ViewModels/Dir1/DirB" we want to ignore "ViewModels/Dir1".
        //
        // Then once we are past the common components, we need to move up
        // a directory for each remaining source path. In this case, we are
        // left with "DirA" so we need a single "..".
        //
        // Then just before we add the remaining target path components we
        // check if there are no path components. If that is the case then
        // we add a single ".".
        //
        // Finally, append any remaining target path components.
        //
        // Examples:
        //
        // Source: ViewModels/DetailBlock/Bags
        // Target: ViewModels/DetailBlock
        // Output: ../
        //
        // Source: ViewModels/DetailBlock
        // Target: Viewmodels/DetailBlock/Bags
        // Output: ./Bags
        //
        // Source: ViewModels/DetailBlock
        // Target: Enums/DetailBlock
        // Output: ../../Enums/DetailBlock

        var pathComponents = new List<string>();

        for ( int i = 0; i < sourcePathComponents.Count && targetPathComponents.Count > 0; i++ )
        {
            if ( sourcePathComponents[i] == targetPathComponents[0] )
            {
                targetPathComponents.RemoveAt( 0 );
            }
            else
            {
                while ( i < sourcePathComponents.Count )
                {
                    pathComponents.Add( ".." );
                    i++;
                }

                break;
            }
        }

        if ( pathComponents.Count == 0 )
        {
            pathComponents.Add( "." );
        }

        pathComponents.AddRange( targetPathComponents );

        return pathComponents;
    }
}
