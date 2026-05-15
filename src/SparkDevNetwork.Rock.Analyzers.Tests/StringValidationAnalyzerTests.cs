using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SparkDevNetwork.Rock.Analyzers.Tests;

public class StringValidationAnalyzerTests
{
    #region Tests

    [Fact]
    public async Task StringPropertyWithDataMember_ReportsError()
    {
        var code = """
        using Rock.Data;
        using System.Runtime.Serialization;
        namespace ThirdParty
        {
            public class Person : Entity<Person>
            {
                [DataMember]
                public string Name { get; set; }
            }
        }
        """;

        var context = new RockAssemblyCSharpAnalyzerTest<StringValidationAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            TestState =
            {
                Sources = { RockCoreAssemblySource }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1001", DiagnosticSeverity.Error ).WithLocation( 8, 23 ) );

        await context.RunAsync();
    }

    [Fact]
    public async Task StringPropertyWithValidationAttribute_DoesNotReportError()
    {
        var code = """
        using Rock.Data;
        using Rock.Security;
        using System.Runtime.Serialization;
        namespace ThirdParty
        {
            public class Person : Entity<Person>
            {
                [DataMember]
                [StringValidation( 0 )]
                public string Name { get; set; }
            }
        }
        """;

        var context = new RockAssemblyCSharpAnalyzerTest<StringValidationAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            TestState =
            {
                Sources = { RockCoreAssemblySource }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        await context.RunAsync();
    }

    [Fact]
    public async Task StringPropertyWithoutDataMember_DoesNotReportError()
    {
        var code = """
        using Rock.Data;
        using System.Runtime.Serialization;
        namespace ThirdParty
        {
            public class Person : Entity<Person>
            {
                public string Name { get; set; }
            }
        }
        """;

        var context = new RockAssemblyCSharpAnalyzerTest<StringValidationAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            TestState =
            {
                Sources = { RockCoreAssemblySource }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        await context.RunAsync();
    }

    [Fact]
    public async Task StringPropertyWithNotMapped_DoesNotReportError()
    {
        var code = """
        using Rock.Data;
        using System.Runtime.Serialization;
        using System.ComponentModel.DataAnnotations.Schema;
        namespace ThirdParty
        {
            public class Person : Entity<Person>
            {
                [DataMember]
                [NotMapped]
                public string Name { get; set; }
            }
        }
        """;

        var context = new RockAssemblyCSharpAnalyzerTest<StringValidationAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            TestState =
            {
                Sources = { RockCoreAssemblySource }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        await context.RunAsync();
    }

    [Fact]
    public async Task StringPropertyWithDataMemberButNoRockSupport_DoesNotReportError()
    {
        var code = """
        using Rock.Data;
        using System.Runtime.Serialization;
        namespace ThirdParty
        {
            public class Person : Entity<Person>
            {
                [DataMember]
                public string Name { get; set; }
            }
        }
        """;

        var context = new RockAssemblyCSharpAnalyzerTest<StringValidationAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            TestState =
            {
                Sources = { RockCoreAssemblySource.Replace( "19.1.0", "18.0.0" ) }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        await context.RunAsync();
    }

    [Fact]
    public async Task StringPropertyWithoutEntity_DoesNotReportError()
    {
        var code = """
        using Rock.Data;
        using System.Runtime.Serialization;
        namespace ThirdParty
        {
            public class Person
            {
                [DataMember]
                public string Name { get; set; }
            }
        }
        """;

        var context = new RockAssemblyCSharpAnalyzerTest<StringValidationAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            TestState =
            {
                Sources = { RockCoreAssemblySource }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        await context.RunAsync();
    }

    [Fact]
    public async Task StringPropertyWithoutSetter_DoesNotReportError()
    {
        var code = """
        using Rock.Data;
        using System.Runtime.Serialization;
        namespace ThirdParty
        {
            public class Person : Entity<Person>
            {
                [DataMember]
                public string Name { get; }
            }
        }
        """;

        var context = new RockAssemblyCSharpAnalyzerTest<StringValidationAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            TestState =
            {
                Sources = { RockCoreAssemblySource }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        await context.RunAsync();
    }

    [Fact]
    public async Task StringPropertyWithPrivateSetter_DoesNotReportError()
    {
        var code = """
        using Rock.Data;
        using System.Runtime.Serialization;
        namespace ThirdParty
        {
            public class Person : Entity<Person>
            {
                [DataMember]
                public string Name { get; private set; }
            }
        }
        """;

        var context = new RockAssemblyCSharpAnalyzerTest<StringValidationAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            TestState =
            {
                Sources = { RockCoreAssemblySource }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        await context.RunAsync();
    }

    [Fact]
    public async Task StringPropertyWithoutRock_DoesNotReportError()
    {
        var code = """
        using System.Runtime.Serialization;
        namespace ThirdParty
        {
            public class Person
            {
                [DataMember]
                public string Name { get; set; }
            }
        }
        """;

        var context = new RockAssemblyCSharpAnalyzerTest<StringValidationAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        await context.RunAsync();
    }

    [Fact]
    public async Task IntPropertyWithDataMember_DoesNotReportError()
    {
        var code = """
        using Rock.Data;
        using System.Runtime.Serialization;
        namespace ThirdParty
        {
            public class Person : Entity<Person>
            {
                [DataMember]
                public int Age { get; set; }
            }
        }
        """;

        var context = new RockAssemblyCSharpAnalyzerTest<StringValidationAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            TestState =
            {
                Sources = { RockCoreAssemblySource }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        await context.RunAsync();
    }

    #endregion

    private static string RockCoreAssemblySource = """
        [assembly: System.Reflection.AssemblyVersion( "19.1.0" )]
        [assembly: System.Reflection.AssemblyFileVersion( "19.1.0" )]
        namespace Rock.Data
        {
            public interface IEntity
            {
            }

            public abstract class Entity<T> : IEntity
                where T : Entity<T>, new()
            {
            }
        }

        namespace Rock.Security
        {
            public class StringValidationAttribute : System.Attribute
            {
                public StringValidationAttribute( int profile )
                {
                }
            }
        }
        """;
    // private static async Task<MetadataReference> CreateRockAssemblyAsync( bool excludeStringValidationAttribute = false )
    // {
    //     // The C# source code for the Rock assembly at version 19.1.0.
    //     string dynamicSource = """
    //         [assembly: System.Reflection.AssemblyVersion( "19.1.0" )]
    //         [assembly: System.Reflection.AssemblyFileVersion( "19.1.0" )]
    //         namespace Rock.Data
    //         {
    //             public interface IEntity
    //             {
    //             }

    //             public abstract class Entity<T> : IEntity
    //                 where T : Entity<T>, new()
    //             {
    //             }
    //         }

    //         namespace Rock.Security
    //         {
    //             public class StringValidationAttribute : System.Attribute
    //             {
    //                 public StringValidationAttribute( int profile )
    //                 {
    //                 }
    //             }
    //         }
    //         """;

    //     if ( excludeStringValidationAttribute )
    //     {
    //         dynamicSource = dynamicSource.Replace( "19.1.0", "18.0.0" );
    //     }

    //     // Create a SyntaxTree from the source code.
    //     SyntaxTree dynamicSyntaxTree = CSharpSyntaxTree.ParseText( dynamicSource );

    //     // Create a compilation for your dynamic code.
    //     var compilation = CSharpCompilation.Create(
    //         "Rock",
    //         [dynamicSyntaxTree],
    //         await ReferenceAssemblies.Net.Net80.ResolveAsync( null, default ),
    //         new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary )
    //     );

    //     // Emit the compilation to a MemoryStream.
    //     var assemblyBytes = new MemoryStream();
    //     var emitResult = compilation.Emit( assemblyBytes );

    //     if ( !emitResult.Success )
    //     {
    //         // Handle compilation errors here.
    //         // For a unit test, you might throw an exception.
    //         throw new InvalidOperationException( "Failed to compile dynamic assembly." );
    //     }

    //     assemblyBytes.Seek( 0, SeekOrigin.Begin );

    //     return MetadataReference.CreateFromStream( assemblyBytes );
    // }

    private class RockAssemblyCSharpAnalyzerTest<TAnalyzer, TVerifier> : CSharpAnalyzerTest<TAnalyzer, TVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TVerifier : IVerifier, new()
    {
        protected override string DefaultTestProjectName => "Rock";
    }
}
