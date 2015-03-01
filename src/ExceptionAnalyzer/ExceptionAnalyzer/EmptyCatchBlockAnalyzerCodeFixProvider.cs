using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace ExceptionAnalyzer
{
    [ExportCodeFixProvider("EmptyCatchBlockAnalyzerCodeFixProvider", LanguageNames.CSharp), Shared]
    public class EmptyCatchBlockAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(EmptyCatchBlockAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            // Create a new block with a list that contains a throw statement.
            var throwStatement = SyntaxFactory.ThrowStatement();
            var statementList = new SyntaxList<StatementSyntax>().Add(throwStatement);
            var newBlock = SyntaxFactory.Block().WithStatements(statementList);

            // Create a new, replacement catch block with our throw statement.
            var newCatchBlock = SyntaxFactory.CatchClause().WithBlock(newBlock).
              WithAdditionalAnnotations(Formatter.Annotation);

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var token = root.FindToken(diagnosticSpan.Start); // This is catch keyword.
            var catchBlock = token.Parent as CatchClauseSyntax; // This is catch block.

            var rr = catchBlock.Block.Statements.Add(throwStatement);
            newBlock = SyntaxFactory.Block().WithStatements(rr).WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(catchBlock.Block, newBlock); // Create new AST.
            //var newRoot = root.ReplaceNode(catchBlock, newCatchBlock); // Create new AST.

            var codeAction = CodeAction.Create("throw", context.Document.WithSyntaxRoot(newRoot));
            context.RegisterFix(codeAction, diagnostic);
        }

    }
}