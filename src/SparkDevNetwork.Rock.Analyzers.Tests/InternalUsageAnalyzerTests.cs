using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CSharp;

namespace SparkDevNetwork.Rock.Analyzers.Tests;

public class InternalUsageAnalyzerTests
{
    #region Tests

    [Fact]
    public async Task CreatingInternalNamespaceInstance_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            new Rock.Foo.Internal.InternalClass();
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 8, 17 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task DefiningInternalNamespaceVariable_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            Rock.Foo.Internal.InternalClass instance = null;
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 8, 13 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task DefiningImplicitInternalNamespaceVariable_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            var instance = new Rock.Foo.Internal.InternalClass();
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 8, 13 ) );
        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 8, 32 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task CreatingRockInternalInstance_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            object instance = new Rock.Foo.InternalClass();
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 8, 35 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task CreatingPublicInstance_DoesNotReportError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            object instance = new Rock.Foo.PublicClass();
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task CreatingPublicInstanceWithInternalConstructor_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            object instance = new Rock.Foo.PublicClass( 4 );
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 8, 35 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task AccessingRockInternalProperty_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            var instance = new Rock.Foo.PublicClass();
            instance.InternalProperty = 5;
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 9, 22 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task AccessingPublicProperty_DoesNotReportError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            var instance = new Rock.Foo.PublicClass();
            instance.PublicProperty = 5;
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task AccessingRockInternalField_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            var instance = new Rock.Foo.PublicClass();
            instance.InternalField = 5;
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 9, 22 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task AccessingPublicField_DoesNotReportError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            var instance = new Rock.Foo.PublicClass();
            instance.PublicField = 5;
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task AccessingRockInternalEvent_ReportsError()
    {
        var code = @"
    namespace ThirdParty
    {
        public class Consumer
        {
            public void Test()
            {
                var instance = new Rock.Foo.PublicClass();
                instance.InternalEvent += (_, _) => { };
            }
        }
    }
    ";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 9, 26 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task AccessingPublicEvent_DoesNotReportError()
    {
        var code = @"
    namespace ThirdParty
    {
        public class Consumer
        {
            public void Test()
            {
                var instance = new Rock.Foo.PublicClass();
                instance.PublicEvent += (_, _) => { };
            }
        }
    }
    ";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task CallingRockInternalMethod_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            var instance = new Rock.Foo.PublicClass();
            instance.InternalMethod();
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 9, 22 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task CallingPublicMethod_DoesNotReportError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            var instance = new Rock.Foo.PublicClass();
            instance.PublicMethod();
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task ReferencingRockInternalMethod_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            var instance = new Rock.Foo.PublicClass();
            var m = instance.InternalMethod;
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 9, 30 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task ReferencingPublicMethod_DoesNotReportError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            var instance = new Rock.Foo.PublicClass();
            var m = instance.PublicMethod;
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task TypeOfRockInternalClass_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            var type = typeof( Rock.Foo.InternalClass );
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 8, 32 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task TypeOfPublicClass_DoesNotReportError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            var type = typeof( Rock.Foo.PublicClass );
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task CallingGenericMethodWithRockInternalClass_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            GenericMethod<Rock.Foo.InternalClass>();
        }

        public void GenericMethod<T>()
        {
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 8, 13 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task CallingGenericMethodWithPublicClass_DoesNotReportError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test()
        {
            GenericMethod<Rock.Foo.PublicClass>();
        }

        public void GenericMethod<T>()
        {
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task DefiningRockInternalSubclass_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer : Rock.Foo.InternalClass
    {
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 4, 29 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task DefiningPublicSubclass_DoesNotReportError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer : Rock.Foo.PublicClass
    {
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task ImplementingRockInternalInterface_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer : Rock.Foo.IInternalInterface
    {
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 4, 18 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task ImplementingPublicInterface_DoesNotReportError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer : Rock.Foo.IPublicInterface
    {
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task DefiningMethodReturningInternalType_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public Rock.Foo.InternalClass Test()
        {
            return null;
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 6, 16 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task DefiningMethodReturningPublicType_DoesNotReportError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public Rock.Foo.PublicClass Test()
        {
            return null;
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task DefiningMethodAcceptingInternalType_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test( Rock.Foo.InternalClass value )
        {
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 6, 27 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task DefiningMethodAcceptingPublicType_DoesNotReportError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public void Test( Rock.Foo.PublicClass value )
        {
        }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task DefiningFieldWithInternalType_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public Rock.Foo.InternalClass Test;
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 6, 16 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task DefiningFieldWithPublicType_DoesNotReportError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public Rock.Foo.PublicClass Test;
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task DefiningPropertyWithInternalType_ReportsError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public Rock.Foo.InternalClass Test { get; set; }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.ExpectedDiagnostics.Add( new DiagnosticResult( "RK1000", DiagnosticSeverity.Warning ).WithLocation( 6, 16 ) );
        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    [Fact]
    public async Task DefiningPropertyWithPublicType_DoesNotReportError()
    {
        var code = @"
namespace ThirdParty
{
    public class Consumer
    {
        public Rock.Foo.PublicClass Test { get; set; }
    }
}
";

        var context = new CSharpAnalyzerTest<InternalUsageAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        context.TestState.AdditionalReferences.Add( await CreateRockAssemblyAsync() );

        await context.RunAsync();
    }

    #endregion

    private static async Task<MetadataReference> CreateRockAssemblyAsync()
    {
        // The C# source code for your dynamic assembly.
        string dynamicSource = @"
namespace Rock.Attribute
{
    public class RockInternalAttribute : System.Attribute
    {
    }
}

namespace Rock.Foo.Internal
{
    public class InternalClass
    {
    }
}

namespace Rock.Foo
{
    [Rock.Attribute.RockInternal]
    public class InternalClass
    {
    }

    public class PublicClass
    {
        public int PublicField;

        [Rock.Attribute.RockInternal]
        public int InternalField;

        public int PublicProperty { get; set; }

        [Rock.Attribute.RockInternal]
        public int InternalProperty { get; set; }

        public event System.EventHandler PublicEvent;

        [Rock.Attribute.RockInternal]
        public event System.EventHandler InternalEvent;

        public PublicClass()
        {
            PublicProperty = 0;
            InternalProperty = 0;
        }

        [Rock.Attribute.RockInternal]
        public PublicClass(int value)
        {
            PublicProperty = value;
            InternalProperty = value;
        }

        public void PublicMethod()
        {
        }

        [Rock.Attribute.RockInternal]
        public void InternalMethod()
        {
        }

        public PublicClass ReturnsPublicType()
        {
            return null;
        }

        public InternalClass ReturnsInternalType()
        {
            return null;
        }

        public void AcceptsPublicType( PublicClass value )
        {
        }

        public void AcceptsInternalType( InternalClass value )
        {
        }
    }

    [Rock.Attribute.RockInternal]
    public interface IInternalInterface
    {
    }

    public interface IPublicInterface
    {
    }
}
";

        // Create a SyntaxTree from the source code.
        SyntaxTree dynamicSyntaxTree = CSharpSyntaxTree.ParseText( dynamicSource );

        // Create a compilation for your dynamic code.
        var compilation = CSharpCompilation.Create(
            "Rock.dll",
            [dynamicSyntaxTree],
            await ReferenceAssemblies.Net.Net80.ResolveAsync( null, default ),
            new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary )
        );

        // Emit the compilation to a MemoryStream.
        var assemblyBytes = new MemoryStream();
        var emitResult = compilation.Emit( assemblyBytes );

        if ( !emitResult.Success )
        {
            // Handle compilation errors here.
            // For a unit test, you might throw an exception.
            throw new InvalidOperationException( "Failed to compile dynamic assembly." );
        }

        assemblyBytes.Seek( 0, SeekOrigin.Begin );

        return MetadataReference.CreateFromStream( assemblyBytes );
    }
}
