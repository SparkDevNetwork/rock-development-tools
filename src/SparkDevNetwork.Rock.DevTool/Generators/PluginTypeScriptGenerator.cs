using SparkDevNetwork.Rock.CodeGenerator;

namespace SparkDevNetwork.Rock.DevTool.Generators;

/// <summary>
/// Handles plugin code generation for TypeScript files.
/// </summary>
internal class PluginTypeScriptGenerator : TypeScriptViewModelGenerator
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

        if ( namespaceComponents[0] == "Rock" )
        {
            return base.GetTypeScriptTypeDefinition( type, isRequired );
        }

        if ( type.IsClass && !type.IsNested && namespaceComponents.Contains( "ViewModels" ) && ( type.Name.EndsWith( "Bag" ) || type.Name.EndsWith( "Box" ) ) )
        {
            var imports = new List<TypeScriptImport>();

            namespaceComponents = namespaceComponents.SkipWhile( c => c != "ViewModels" ).ToList();

            if ( !namespaceComponents.SequenceEqual( _targetPathComponents ) )
            {
                var pathComponents = GetPathReferenceComponents( [.. _targetPathComponents], [.. namespaceComponents] );
                var path = string.Join( "/", pathComponents );

                imports.Add( new TypeScriptImport
                {
                    SourcePath = path,
                    NamedImport = type.Name
                } );
            }

            var tsType = isRequired ? type.Name : $"{type.Name} | null";

            return new TypeScriptTypeDefinition( tsType, imports );
        }
        else if ( namespaceComponents.Contains( "Enums" ) && type.IsEnum )
        {
            var imports = new List<TypeScriptImport>();

            namespaceComponents = namespaceComponents.SkipWhile( c => c != "Enums" ).ToList();

            if ( !namespaceComponents.SequenceEqual( _targetPathComponents ) )
            {
                var pathComponents = GetPathReferenceComponents( [.. _targetPathComponents], [.. namespaceComponents] );
                var path = $"{string.Join( "/", pathComponents )}.partial";

                imports.Add( new TypeScriptImport
                {
                    SourcePath = path,
                    NamedImport = type.Name
                } );
            }

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
    internal static List<string> GetPathReferenceComponents( List<string> sourcePathComponents, List<string> targetPathComponents )
    {
        // Special case, if the two paths are the same, throw an exception
        // because we should never be called this way.
        if ( sourcePathComponents.SequenceEqual( targetPathComponents ) )
        {
            throw new ArgumentException( "Source and target paths must not be the same." );
        }

        var pathComponents = new List<string>();
        string? appendComponent = null;

        while ( sourcePathComponents.Count > 0 )
        {
            if ( targetPathComponents.Count > 0 && sourcePathComponents[0] == targetPathComponents[0] )
            {
                if ( targetPathComponents.Count == 1 )
                {
                    appendComponent = targetPathComponents[0].ToCamelCase();
                }

                var isViewModelsPrependRequired = sourcePathComponents.Count == 1
                    && sourcePathComponents[0] == "ViewModels"
                    && targetPathComponents[0] == "ViewModels";

                // Because of our slightly wonky folder structure, references
                // from the root ViewModels namespace to a child namespace need
                // an additional "./ViewModels/" in the path.
                if ( isViewModelsPrependRequired )
                {
                    pathComponents.Add( "." );
                    pathComponents.Add( "ViewModels" );
                }

                sourcePathComponents.RemoveAt( 0 );
                targetPathComponents.RemoveAt( 0 );
            }
            else
            {
                for ( int i = 0; i < sourcePathComponents.Count - 1; i++ )
                {
                    pathComponents.Add( ".." );
                }

                if ( targetPathComponents.Count == 0 )
                {
                    pathComponents.Add( ".." );
                }

                break;
            }
        }

        if ( pathComponents.Count == 0 )
        {
            pathComponents.Add( "." );
        }

        if ( appendComponent != null )
        {
            pathComponents.Add( appendComponent );
        }

        if ( targetPathComponents.Count > 1 )
        {
            pathComponents.AddRange( targetPathComponents[..^1] );
        }

        if ( targetPathComponents.Count > 0 )
        {
            pathComponents.Add( targetPathComponents.Last().ToCamelCase() );
        }

        return pathComponents;
    }
}
