using SparkDevNetwork.Rock.CodeGenerator;

namespace SparkDevNetwork.Rock.DevTool.Generators;

/// <summary>
/// Handles plugin code generation for TypeScript files.
/// </summary>
class PluginTypeScriptGenerator : TypeScriptViewModelGenerator
{
    /// <inheritdoc/>
    protected override TypeScriptTypeDefinition? GetTypeScriptTypeDefinition( Type type, bool isRequired )
    {
        if ( type.Namespace == "tester" && type.Name.EndsWith( "Bag" ) )
        {
            var path = $"../{type.Name.ToCamelCase()}"; //$"{type.Namespace.Substring(15).Trim('.').Replace('.', '/')}/{type.Name.ToCamelCase()}";

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
        else if ( type.Namespace == "tester" && type.IsEnum )
        {
            var path = $"../{type.Name.ToCamelCase()}";

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
}
