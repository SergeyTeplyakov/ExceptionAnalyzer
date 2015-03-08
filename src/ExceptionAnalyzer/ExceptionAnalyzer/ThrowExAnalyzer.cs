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
        public const string DiagnosticId = "Rethrowing exception is possibly intended";
        // TODO: extract all messages somewhere to be able to add errogant messages
        internal const string Title = "Rethrow exceptions properly, dude!";
        public const string MessageFormat = "Rethrowing exception is possibly intended";
        internal const string Category = "CodeSmell";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

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

            // This approach is not working!!!
            // var catchExSymbolInfo = context.SemanticModel.GetSymbolInfo(exceptionDeclarationIdentifier.Parent);

            foreach (var throwStatement in catchClause.DescendantNodes().OfType<ThrowStatementSyntax>())
            {
                var identifier =
                    throwStatement
                    .Expression.As(x => x as IdentifierNameSyntax);

                if (identifier == null)
                    continue;

                // Naive approach!!
                if (identifier.Identifier.Text == exceptionDeclarationIdentifier.Text)
                {
                    // throw ex; detected!
                    var diagnostic = Diagnostic.Create(Rule, identifier.GetLocation());

                    context.ReportDiagnostic(diagnostic);
                }

                // TODO: Experiments!!
                //var semanticModel = context.SemanticModel as Microsoft.CodeAnalysis.CSharp.SyntaxTreeSemanticModel;
                //var semanticModel = context.
                var symbolInfo = context.SemanticModel.GetSymbolInfo(identifier);
                
                //// Always null!!
                //var dec = anotherSyntaxModel.GetDeclaredSymbol(identifier);
                //var dec2 = anotherSyntaxModel.GetDeclaredSymbol(identifier.Parent);

                // GetDeclaredSymbol always returns null! Why!&!&
                //var declared = context.SemanticModel.GetDeclaredSymbol(identifier);
            }
        }
    }
}
