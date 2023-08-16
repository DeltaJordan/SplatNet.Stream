using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#pragma warning disable RS1024
#pragma warning disable RS1004
#pragma warning disable RS2008

namespace SplatNet.CodeStyle.Analyzers
{

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SafeAsyncAnalyzer : DiagnosticAnalyzer
    {

        public static readonly DiagnosticDescriptor s_unsafeAsync = new DiagnosticDescriptor(
            id: "AKI001",
            title: "Awaited task not using SafeAsync()",
            messageFormat: "Awaited task not using SafeAsync()",
            category: "Correctness",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Awaited tasks should call SafeAsync() to avoid deadlocks."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_unsafeAsync);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(RegisterAnalyzer);
        }

        public static void RegisterAnalyzer(CompilationStartAnalysisContext context)
        {
            ImmutableHashSet<INamedTypeSymbol> safeTaskTypes = new INamedTypeSymbol[]{
                context.Compilation.GetTypeByMetadataName( "System.Runtime.CompilerServices.ConfiguredTaskAwaitable" ),
                context.Compilation.GetTypeByMetadataName( "System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1" ),
                context.Compilation.GetTypeByMetadataName( "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable" ),
                context.Compilation.GetTypeByMetadataName( "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable`1" )
            }.Where(
                x => x != null &&
                x.Kind != SymbolKind.ErrorType
            ).ToImmutableHashSet();

            if (!safeTaskTypes.Any())
            {
                return;
            }

            context.RegisterOperationAction(op =>
            {
                IAwaitOperation awaitOp = op.Operation as IAwaitOperation;
                ITypeSymbol awaitedType = awaitOp.Operation.Type.OriginalDefinition;

                if (safeTaskTypes.Contains(awaitedType))
                {
                    return;
                }

                op.ReportDiagnostic(Diagnostic.Create(
                    s_unsafeAsync,
                    awaitOp.Syntax.GetLocation()
                ));
            }, OperationKind.Await);
        }

    }

}

#pragma warning restore RS1024
#pragma warning restore RS1004
#pragma warning restore RS2008
