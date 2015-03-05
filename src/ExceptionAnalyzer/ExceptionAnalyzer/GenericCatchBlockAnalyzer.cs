using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExceptionAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class GenericCatchBlockAnalyzer : DiagnosticAnalyzer
    {
        // Catch is not empty, `catch` or `catch(Exception)` and some return statement exists. Add hint!
        // show hint on the return itself
        public const string DiagnosticId = "Generic catch block analyzer";
        internal const string Title = "Swallow all exceptions considered harmful";
        internal const string MessageFormat = "Catching everything, app could be in unpredictable state";
        internal const string Category = "CodeSmell";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.CatchClause);
        }

        // Called when Roslyn encounters a catch clause.
        private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var catchBlock = context.Node as CatchClauseSyntax;
            // Current analyzer is interested in "catch {}" blocks without exception declarations
            if (catchBlock == null || catchBlock.Declaration != null)
            {
                return;
            }

            StatementSyntax syntax = catchBlock.Block;
            var controlFlow = context.SemanticModel.AnalyzeControlFlow(syntax);

            // Warn if end block is reachable or there is a return statement
            if (controlFlow.EndPointIsReachable || controlFlow.ReturnStatements.Length != 0)
            {
                // Block is empty, create and report diagnostic warning.
                var diagnostic = Diagnostic.Create(Rule, catchBlock.CatchKeyword.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
