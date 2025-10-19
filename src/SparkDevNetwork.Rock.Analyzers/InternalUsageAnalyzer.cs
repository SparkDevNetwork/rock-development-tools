using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

// The concepts for many of the internals to this class have been taken from
// the Entity Framework Core analyzers since we are trying to do essentially
// the same thing.

namespace SparkDevNetwork.Rock.Analyzers
{
    /// <summary>
    /// Analyzer that checks for usage of APIs that have been marked as
    /// internal. This provides feedback to the developer that the thing
    /// they are using might break with any update.
    /// </summary>
    [DiagnosticAnalyzer( LanguageNames.CSharp )]
    public class InternalUsageAnalyzer : DiagnosticAnalyzer
    {
        #region Constants

        /// <summary>
        /// The message format to use when reporting diagnostic messages
        /// from this analyzer.
        /// </summary>
        public const string MessageFormat
            = "{0} is an internal API that supports the Rock infrastructure and "
            + "not subject to the same compatibility standards as public APIs. "
            + "It may be changed or removed without notice in any release.";

        /// <summary>
        /// The rule that will be reported for any violations found.
        /// </summary>
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: "RK1000",
            title: "Internal Rock API usage",
            messageFormat: MessageFormat,
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true );

        #endregion

        #region Properties

        /// <summary>
        /// Returns a set of descriptors for the diagnostics that this analyzer
        /// is capable of producing.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( Rule );

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

            context.RegisterOperationAction( AnalyzeOperation,
                OperationKind.FieldReference,
                OperationKind.PropertyReference,
                OperationKind.MethodReference,
                OperationKind.EventReference,
                OperationKind.Invocation,
                OperationKind.ObjectCreation,
                OperationKind.VariableDeclaration,
                OperationKind.TypeOf );

            context.RegisterSymbolAction( AnalyzeSymbol,
                SymbolKind.NamedType,
                SymbolKind.Method,
                SymbolKind.Property,
                SymbolKind.Field,
                SymbolKind.Event );
        }

        /// <summary>
        /// Analyzes the operation being performed.
        /// </summary>
        /// <param name="context">The context.</param>
        private static void AnalyzeOperation( OperationAnalysisContext context )
        {
            try
            {
                if ( context.Operation is IFieldReferenceOperation fieldReference )
                {
                    AnalyzeMember( context, fieldReference.Field );
                }
                else if ( context.Operation is IPropertyReferenceOperation propertyReference )
                {
                    AnalyzeMember( context, propertyReference.Property );
                }
                else if ( context.Operation is IEventReferenceOperation eventReference )
                {
                    AnalyzeMember( context, eventReference.Event );
                }
                else if ( context.Operation is IMethodReferenceOperation methodReference )
                {
                    AnalyzeMember( context, methodReference.Method );
                }
                else if ( context.Operation is IObjectCreationOperation objectCreation )
                {
                    AnalyzeMember( context, objectCreation.Constructor );
                }
                else if ( context.Operation is IVariableDeclarationOperation variableDeclaration )
                {
                    AnalyzeVariableDeclaration( context, variableDeclaration );
                }
                else if ( context.Operation is IInvocationOperation invocation )
                {
                    AnalyzeInvocation( context, invocation );
                }
                else if ( context.Operation is ITypeOfOperation typeOf )
                {
                    AnalyzeTypeOf( context, typeOf );
                }
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.WriteLine( $"{ex.Message}: {ex.StackTrace}" );
            }
        }

        /// <summary>
        /// Analyzes the member access to see if it is internal API usage.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="symbol">The symbol to be checked.</param>
        private static void AnalyzeMember( OperationAnalysisContext context, ISymbol symbol )
        {
            var containingType = symbol.ContainingType;

            // Check if the member has been marked as internal.
            if ( HasInternalAttribute( symbol ) )
            {
                // Special logic if the member is a constructor, otherwise show
                // the full member name.
                ReportDiagnostic( context, symbol.Name == ".ctor" ? containingType.Name : $"{containingType}.{symbol.Name}" );
            }

            // Check if the type that contains the member is marked internal.
            if ( IsInternal( context, containingType ) )
            {
                ReportDiagnostic( context, containingType );
            }
        }

        /// <summary>
        /// Analyzes the variable declaration for internal API usage.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="variableDeclaration">The variable declaration.</param>
        private static void AnalyzeVariableDeclaration( OperationAnalysisContext context, IVariableDeclarationOperation variableDeclaration )
        {
            foreach ( var declarator in variableDeclaration.Declarators )
            {
                if ( IsInternal( context, declarator.Symbol.Type ) && context.Operation.Syntax is VariableDeclarationSyntax vds )
                {
                    var diagnostic = Diagnostic.Create( Rule, vds.Type.GetLocation(), declarator.Symbol.Type );

                    context.ReportDiagnostic( diagnostic );
                    return;
                }
            }
        }

        /// <summary>
        /// Analyzes the invocation of a method to see if it is internal.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="invocation">The invocation.</param>
        private static void AnalyzeInvocation( OperationAnalysisContext context, IInvocationOperation invocation )
        {
            // Check if the method being called has any type arguments that
            // are internal.
            foreach ( var a in invocation.TargetMethod.TypeArguments )
            {
                if ( IsInternal( context, a ) )
                {
                    var diagnostic = Diagnostic.Create( Rule, context.Operation.Syntax.GetLocation(), a );
                    context.ReportDiagnostic( diagnostic );
                }
            }

            // Check the method itself.
            AnalyzeMember( context, invocation.TargetMethod );
        }

        /// <summary>
        /// Analyzes the typeof operator usage to see if it is using an internal
        /// API type.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="typeOf">The type of operation.</param>
        private static void AnalyzeTypeOf( OperationAnalysisContext context, ITypeOfOperation typeOf )
        {
            if ( IsInternal( context, typeOf.TypeOperand ) )
            {
                ReportDiagnostic( context, typeOf.TypeOperand );
            }
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
                if ( context.Symbol is INamedTypeSymbol namedType )
                {
                    AnalyzeNamedTypeSymbol( context, namedType );
                }
                else if ( context.Symbol is IMethodSymbol method )
                {
                    AnalyzeMethodSymbol( context, method );
                }
                else if ( context.Symbol is IFieldSymbol field )
                {
                    AnalyzeMemberSymbol( context, field, field.Type );
                }
                else if ( context.Symbol is IPropertySymbol property )
                {
                    AnalyzeMemberSymbol( context, property, property.Type );
                }
                else if ( context.Symbol is IEventSymbol @event )
                {
                    AnalyzeMemberSymbol( context, @event, @event.Type );
                }
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.WriteLine( $"{ex.Message}: {ex.StackTrace}" );
            }
        }

        /// <summary>
        /// Analyzes the definitions of new types to see if they are making
        /// use of any internal APIs that they shouldn't.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="namedType">The named type being declared.</param>
        private static void AnalyzeNamedTypeSymbol( SymbolAnalysisContext context, INamedTypeSymbol namedType )
        {
            // Check if the base type is an internal API.
            if ( namedType.BaseType is ITypeSymbol baseSymbol && IsInternal( context, baseSymbol ) )
            {
                foreach ( var declaringSyntax in namedType.DeclaringSyntaxReferences )
                {
                    Location location;

                    // I'll be honest, I'm not sure exactly what this is doing here.
                    // I think it's looking for the character position in the line
                    // of the specific reference to the base type.
                    if ( declaringSyntax.GetSyntax() is ClassDeclarationSyntax classDeclaration && classDeclaration.BaseList?.Types.Count > 0 )
                    {
                        location = classDeclaration.BaseList.Types[0].GetLocation();

                        var diagnostic = Diagnostic.Create( Rule, location, baseSymbol );

                        context.ReportDiagnostic( diagnostic );
                    }
                }
            }

            // Check each of the interfaces being used by the new type.
            foreach ( var iface in namedType.Interfaces.Where( i => IsInternal( context, i ) ) )
            {
                foreach ( var declaringSyntax in namedType.DeclaringSyntaxReferences )
                {
                    Location location;

                    // Find the location in the line of the reference to the
                    // interface that is internal.
                    if ( declaringSyntax.GetSyntax() is ClassDeclarationSyntax classDeclaration )
                    {
                        location = classDeclaration.Identifier.GetLocation();

                        var diagnostic = Diagnostic.Create( Rule, location, iface );

                        context.ReportDiagnostic( diagnostic );
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the method declaration to see if the return type or
        /// parameters are trying to make use of any internal types.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="method">The method declaration.</param>
        private static void AnalyzeMethodSymbol( SymbolAnalysisContext context, IMethodSymbol method )
        {
            // Skip property access methods as they are handled elsewhere.
            if ( method.MethodKind == MethodKind.PropertyGet || method.MethodKind == MethodKind.PropertySet )
            {
                return;
            }

            // If the method returns an internal type then generate a
            // diagnostic message.
            if ( IsInternal( context, method.ReturnType ) )
            {
                foreach ( var declaringSyntax in method.DeclaringSyntaxReferences )
                {
                    Location location;

                    if ( declaringSyntax.GetSyntax() is MethodDeclarationSyntax methodDeclaration )
                    {
                        location = methodDeclaration.ReturnType.GetLocation();

                        var diagnostic = Diagnostic.Create( Rule, location, method.ReturnType );

                        context.ReportDiagnostic( diagnostic );
                    }
                }
            }

            // Check each parameter of the method and see if any are internal
            // API types.
            foreach ( var paramSymbol in method.Parameters.Where( ps => IsInternal( context, ps.Type ) ) )
            {
                foreach ( var declaringSyntax in paramSymbol.DeclaringSyntaxReferences )
                {
                    Location location;

                    if ( declaringSyntax.GetSyntax() is ParameterSyntax parameter && parameter.Type != null )
                    {
                        location = parameter.Type.GetLocation();

                        var diagnostic = Diagnostic.Create( Rule, location, paramSymbol.Type );

                        context.ReportDiagnostic( diagnostic );
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the member declaration to see if it is being declared with
        /// an internal API type.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="declarationSymbol">The declaration symbol.</param>
        /// <param name="typeSymbol">The type symbol.</param>
        private static void AnalyzeMemberSymbol( SymbolAnalysisContext context, ISymbol declarationSymbol, ITypeSymbol typeSymbol )
        {
            if ( IsInternal( context, typeSymbol ) )
            {
                foreach ( var declaringSyntax in declarationSymbol.DeclaringSyntaxReferences )
                {
                    ReportDiagnostic( context, declaringSyntax.GetSyntax(), typeSymbol );
                }
            }
        }

        /// <summary>
        /// Reports the diagnostic message for the operation.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="messageArg">The message argument.</param>
        private static void ReportDiagnostic( OperationAnalysisContext context, object messageArg )
        {
            var location = NarrowDownSyntax( context.Operation.Syntax ).GetLocation();
            var diagnostic = Diagnostic.Create( Rule, location, messageArg );

            context.ReportDiagnostic( diagnostic );
        }

        /// <summary>
        /// Reports the diagnostic message for the syntax node.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="syntax">The syntax.</param>
        /// <param name="messageArg">The message argument.</param>
        private static void ReportDiagnostic( SymbolAnalysisContext context, SyntaxNode syntax, object messageArg )
        {
            var location = NarrowDownSyntax( syntax ).GetLocation();
            var diagnostic = Diagnostic.Create( Rule, location, messageArg );

            context.ReportDiagnostic( diagnostic );
        }

        /// <summary>
        /// Attempt to find a more specific syntax node to report to the user.
        /// This allows us to provide better error message line numbers and
        /// character positions.
        /// </summary>
        private static SyntaxNode NarrowDownSyntax( SyntaxNode syntax )
        {
            if ( syntax is InvocationExpressionSyntax invocationExpression && invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessSyntax )
            {
                return memberAccessSyntax.Name;
            }
            else if ( syntax is MemberAccessExpressionSyntax memberAccessExpression )
            {
                return memberAccessExpression.Name;
            }
            else if ( syntax is ObjectCreationExpressionSyntax objectCreationExpression )
            {
                return objectCreationExpression.Type;
            }
            else if ( syntax is PropertyDeclarationSyntax propertyDeclaration )
            {
                return propertyDeclaration.Type;
            }
            else if ( syntax is VariableDeclaratorSyntax variableDeclarator )
            {
                return variableDeclarator.Parent is VariableDeclarationSyntax declaration
                    ? declaration.Type
                    : ( SyntaxNode ) variableDeclarator;
            }
            else if ( syntax is TypeOfExpressionSyntax typeOfExpression )
            {
                return typeOfExpression.Type;
            }
            else
            {
                return syntax;
            }
        }

        /// <summary>
        /// Determines whether the specified symbol usage is internal.
        /// </summary>
        /// <param name="context">The context using the symbol.</param>
        /// <param name="symbol">The symbol.</param>
        /// <returns>
        ///   <c>true</c> if the specified symbol usage is internal; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsInternal( SymbolAnalysisContext context, ITypeSymbol symbol )
        {
            return IsCoreAssembly( symbol.ContainingAssembly )
                && !IsCoreAssembly( context.Compilation.Assembly )
                && ( IsInInternalNamespace( symbol ) || HasInternalAttribute( symbol ) );
        }

        /// <summary>
        /// Determines whether the specified symbol usage is internal.
        /// </summary>
        /// <param name="context">The context using the symbol.</param>
        /// <param name="symbol">The symbol.</param>
        /// <returns>
        ///   <c>true</c> if the specified symbol usage is internal; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsInternal( OperationAnalysisContext context, ITypeSymbol symbol )
        {
            return IsCoreAssembly( symbol.ContainingAssembly )
                && !IsCoreAssembly( context.Compilation.Assembly )
                && ( IsInInternalNamespace( symbol ) || HasInternalAttribute( symbol ) );
        }

        /// <summary>
        /// Determines whether the assembly is considered a core assembly
        /// Core assemblies are ones managed and created by the Spark team.
        /// </summary>
        /// <param name="assembly">The assembly to be checked.</param>
        /// <returns>
        ///   <c>true</c> if the assembly is considered core; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsCoreAssembly( IAssemblySymbol assembly )
        {
            return assembly.Name == "Rock" || assembly.Name.StartsWith( "Rock." );
        }

        /// <summary>
        /// Determines whether the symbol has an attribute that specifies
        /// it as internal usage.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>
        ///   <c>true</c> if the symbol has an internal attribute attached; otherwise, <c>false</c>.
        /// </returns>
        private static bool HasInternalAttribute( ISymbol symbol )
        {
            return symbol.GetAttributes()
                .Any( a => a.AttributeClass.ToDisplayString() == "Rock.Attribute.RockInternalAttribute" );
        }

        /// <summary>
        /// Determines whether the symbol is inside an Internal namespace.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>
        ///   <c>true</c> if the symbol is inside an Internal namespace; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsInInternalNamespace( ISymbol symbol )
        {
            var ns = symbol?.ContainingNamespace;

            if ( ns == null || ns.IsGlobalNamespace )
            {
                return false;
            }

            // Collect namespace parts from outermost to innermost.
            var parts = new List<string>();
            var current = ns;

            while ( current != null && !current.IsGlobalNamespace )
            {
                parts.Insert( 0, current.Name );
                current = current.ContainingNamespace;
            }

            // Must be at least 2 segments: "Rock[.*].Internal".
            if ( parts.Count < 2 )
            {
                return false;
            }

            return parts[0] == "Rock" && parts[parts.Count - 1] == "Internal";
        }

        #endregion
    }
}
