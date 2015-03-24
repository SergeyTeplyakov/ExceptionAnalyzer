using ExceptionAnalyzer.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace ExceptionAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ThrowNewExceptionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "EA006";
        internal const string Title = "Add catched exception as inner exception";
        public const string MessageFormat = "Original exception was abandoned in the catch block.\r\nConsider adding original exception as inner exception.";
        internal const string Category = "CodeSmell";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeCatchClause, SyntaxKind.CatchClause);
        }

        // Called when Roslyn encounters a catch clause.
        private static void AnalyzeCatchClause(SyntaxNodeAnalysisContext context)
        {
            var catchClause = context.Node as CatchClauseSyntax;
            //if (catchClause == null || catchClause.Declaration == null)
            if (catchClause == null)
            {
                return;
            }

            // In some cases throw statement could hide original exception.
            // For instance catch block could swallow original excpetion!

            foreach (var throwStatement in catchClause.DescendantNodes().OfType<ThrowStatementSyntax>())
            {
                var creationExpression =
                    throwStatement.Expression as ObjectCreationExpressionSyntax;

                if (creationExpression == null)
                    continue;

                // There should be usage for the ex object
                var exceptionWasUsed = 
                    creationExpression.ArgumentList.Arguments
                    .Where(a => a.Expression != null)
                    .Select(a => new { Argument = a, Symbol = context.SemanticModel.GetSymbolInfo(a.Expression) })
                    .Any(a => a.Symbol.Symbol != null && a.Symbol.Symbol.ExceptionFromCatchBlock());

                if (!exceptionWasUsed)
                {
                    // throw new Exception(); detected!
                    var diagnostic = Diagnostic.Create(Rule, throwStatement.GetLocation());

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
