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
    public sealed class ThrowExAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "EA004";
        // TODO: extract all messages somewhere to be able to add errogant messages
        internal const string Title = "Rethrow exceptions properly, dude!";
        public const string MessageFormat = "Rethrowing exception is possibly intended";
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
            if (catchClause == null || catchClause.Declaration == null)
            {
                return;
            }

            // Looking for "ex" in "catch(Exception ex)"
            var exceptionDeclarationIdentifier = catchClause.Declaration.Identifier;

            // Exception identifier is optional in catch clause. It could be "catch(Exception)"
            if (exceptionDeclarationIdentifier.Kind() == SyntaxKind.None)
            {
                return;
            }

            foreach (var throwStatement in catchClause.DescendantNodes().OfType<ThrowStatementSyntax>())
            {
                var identifier =
                    throwStatement.Expression as IdentifierNameSyntax;

                if (identifier == null)
                    continue;

                var symbol = context.SemanticModel.GetSymbolInfo(identifier);
                if (symbol.Symbol == null)
                    continue;

                if (symbol.Symbol.ExceptionFromCatchBlock())
                {
                    // throw ex; detected!
                    var diagnostic = Diagnostic.Create(Rule, identifier.GetLocation());

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
