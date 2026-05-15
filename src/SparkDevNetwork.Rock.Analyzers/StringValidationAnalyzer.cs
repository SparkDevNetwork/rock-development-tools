using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SparkDevNetwork.Rock.Analyzers
{
    /// <summary>
    /// Analyzer that checks for string properties on entities that are missing
    /// the StringValidation attribute.
    /// </summary>
    [DiagnosticAnalyzer( LanguageNames.CSharp )]
    public class StringValidationAnalyzer : DiagnosticAnalyzer
    {
        #region Constants

        /// <summary>
        /// The message format to use when reporting diagnostic messages
        /// from this analyzer.
        /// </summary>
        public const string MessageFormat = "{0} must be decorated with the StringValidation attribute";

        /// <summary>
        /// The rule that will be reported for any violations found.
        /// </summary>
        private static readonly DiagnosticDescriptor PropertyRule = new DiagnosticDescriptor(
            id: "RK1001",
            title: "Require StringValidation attribute on string properties",
            messageFormat: MessageFormat,
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true );

        #endregion

        #region Properties

        /// <summary>
        /// Returns a set of descriptors for the diagnostics that this analyzer
        /// is capable of producing.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( PropertyRule );

        #endregion

        #region Methods

        /// <summary>
        /// Called once at session start to register actions in the analysis context.
        /// </summary>
        /// <param name="context"></param>
        public override void Initialize( AnalysisContext context )
        {
            context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction( AnalyzeSymbol, SymbolKind.Property );
        }

        /// <summary>
        /// Analyzes the symbol being declared to see if it is making use
        /// of any internal API types.
        /// </summary>
        /// <param name="context">The context.</param>
        private static void AnalyzeSymbol( SymbolAnalysisContext context )
        {
            try
            {
                var rockAssembly = context.Compilation.GetTypeByMetadataName( "Rock.Data.IEntity" )?.ContainingAssembly;

                // Currently only supported in the Rock assembly itself.
                if ( rockAssembly == null || context.Symbol.ContainingAssembly.Identity.Name != "Rock" )
                {
                    return;
                }

                // Feature was fully introduced in 19.1, so skip analysis for older versions.
                if ( rockAssembly.Identity.Version < new Version( 19, 1 ) )
                {
                    return;
                }

                if ( context.Symbol is IPropertySymbol property )
                {
                    AnalyzePropertySymbol( context, property, property.Type );
                }
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.WriteLine( $"{ex.Message}: {ex.StackTrace}" );
            }
        }

        /// <summary>
        /// Analyzes the property declaration to see if it is needs to be
        /// decorated with the StringValidation attribute.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="propertySymbol">The declaration symbol.</param>
        /// <param name="typeSymbol">The type symbol.</param>
        private static void AnalyzePropertySymbol( SymbolAnalysisContext context, IPropertySymbol propertySymbol, ITypeSymbol typeSymbol )
        {
            // Property type must be a string.
            if ( propertySymbol.Type.SpecialType != SpecialType.System_String )
            {
                return;
            }

            // The containing type must implement IEntity.
            if ( !propertySymbol.ContainingType.AllInterfaces.Any( i => i.ToDisplayString() == "Rock.Data.IEntity" ) )
            {
                return;
            }

            var attributes = propertySymbol.GetAttributes();

            // Skip if it is already decorated.
            if ( attributes.Any( a => a.AttributeClass.ToDisplayString() == "Rock.Security.StringValidationAttribute" ) )
            {
                return;
            }

            // It must be decorated with DataMember.
            if ( !attributes.Any( a => a.AttributeClass.ToDisplayString() == "System.Runtime.Serialization.DataMemberAttribute" ) )
            {
                return;
            }

            // It must not be decorated with NotMapped.
            if ( attributes.Any( a => a.AttributeClass.ToDisplayString() == "System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute" ) )
            {
                return;
            }

            // The type must not be decorated with NotMapped.
            if ( propertySymbol.ContainingType.GetAttributes().Any( a => a.AttributeClass.ToDisplayString() == "System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute" ) )
            {
                return;
            }

            // It must have a public setter.
            if ( propertySymbol.SetMethod == null || propertySymbol.SetMethod.DeclaredAccessibility != Accessibility.Public )
            {
                return;
            }

            foreach ( var declaringSyntax in propertySymbol.DeclaringSyntaxReferences )
            {
                ReportDiagnostic( context, declaringSyntax.GetSyntax(), propertySymbol );
            }
        }

        /// <summary>
        /// Reports the diagnostic message for the syntax node.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="syntax">The syntax.</param>
        /// <param name="messageArg">The message argument.</param>
        private static void ReportDiagnostic( SymbolAnalysisContext context, SyntaxNode syntax, object messageArg )
        {
            var location = NarrowDownSyntaxLocation( syntax );
            var diagnostic = Diagnostic.Create( PropertyRule, location, messageArg );

            context.ReportDiagnostic( diagnostic );
        }

        /// <summary>
        /// Attempt to find a more specific syntax node to report to the user.
        /// This allows us to provide better error message line numbers and
        /// character positions.
        /// </summary>
        private static Location NarrowDownSyntaxLocation( SyntaxNode syntax )
        {
            if ( syntax is PropertyDeclarationSyntax propertyDeclaration )
            {
                return propertyDeclaration.Identifier.GetLocation();
            }
            else
            {
                return syntax.GetLocation();
            }
        }

        #endregion
    }
}
