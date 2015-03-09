using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ExceptionAnalyzer
{
    /// <summary>
    /// Analyzes controlf flow for the catch block and warns for every exit-point that swallow an exception.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SwallowExceptionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "EA003";
        internal const string Title = "Catch block swallows an exception";
        internal const string MessageFormat = "Exit point '{0}' swallows an exception!";
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
            // Ignoring non-catch blocks and catch blocks without exception declarations
            if (catchBlock == null || catchBlock.Declaration == null)
            {
                return;
            }

            StatementSyntax syntax = catchBlock.Block;
            var controlFlow = context.SemanticModel.AnalyzeControlFlow(syntax);

            // Warn for every exit points
            foreach(var @return in controlFlow.ExitPoints)
            {
                // Block is empty, create and report diagnostic warning.
                var diagnostic = Diagnostic.Create(Rule, @return.GetLocation(), @return.WithoutTrivia().GetText());
                context.ReportDiagnostic(diagnostic);
            }

            // EndPoint (end of the block) is not a exit point. Should be covered separately!
            if (controlFlow.EndPointIsReachable)
            {
                var diagnostic = Diagnostic.Create(Rule, catchBlock.Block.CloseBraceToken.GetLocation(), catchBlock.Block.CloseBraceToken.ValueText);
                context.ReportDiagnostic(diagnostic);
                
            }
        }
    }
}
