using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable RS1004
#pragma warning disable RS2008

namespace SplatNet.CodeStyle.Analyzers
{

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AsyncMethodNameAnalyzer : DiagnosticAnalyzer
    {

        public static readonly DiagnosticDescriptor s_missingAsync = new DiagnosticDescriptor(
            id: "AKI002",
            title: "Asynchronous method name does not end in Async",
            messageFormat: "Asynchronous method name does not end in Async",
            category: "Naming",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor s_unexpectedAsync = new DiagnosticDescriptor(
            id: "AKI003",
            title: "Synchronous method name ends in Async",
            messageFormat: "Synchronous method name ends in Async",
            category: "Naming",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_missingAsync, s_unexpectedAsync);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(RegisterAnalyzer);
        }

        public static void RegisterAnalyzer(CompilationStartAnalysisContext context)
        {
            context.RegisterSymbolAction(cxt =>
            {
                IMethodSymbol method = cxt.Symbol as IMethodSymbol;
                if (string.IsNullOrEmpty(method?.Name)) return;
                if (method.MetadataName.Equals("Main")) return;
                bool namedAsync = method.MetadataName.EndsWith("Async") || method.MetadataName.EndsWith("Async`");
                bool isAsync = method.IsAsync || ReturnsTask(method);

                if (isAsync && !namedAsync)
                {
                    cxt.ReportDiagnostic(Diagnostic.Create(s_missingAsync, cxt.Symbol.OriginalDefinition.Locations.First()));
                }
                else if (!isAsync && namedAsync)
                {
                    cxt.ReportDiagnostic(Diagnostic.Create(s_unexpectedAsync, cxt.Symbol.OriginalDefinition.Locations.First()));
                }
            }, new SymbolKind[] { SymbolKind.Method });
        }

        private static readonly HashSet<string> s_taskTypes = new HashSet<string>{
            "Task",
            "Task`1",
            "ValueTask",
            "ValueTask`1",
            "ConfiguredTaskAwaitable",
            "ConfiguredTaskAwaitable`1"
        };

        private static bool ReturnsTask(IMethodSymbol method)
        {
            return s_taskTypes.Contains(method.ReturnType.MetadataName);
        }

    }

}

#pragma warning restore RS1004
#pragma warning restore RS2008
