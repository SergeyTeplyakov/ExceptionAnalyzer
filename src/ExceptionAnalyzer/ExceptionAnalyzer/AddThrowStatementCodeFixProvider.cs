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
using System;

namespace ExceptionAnalyzer
{
    [ExportCodeFixProvider("EmptyCatchBlockAnalyzerCodeFixProvider", LanguageNames.CSharp), Shared]
    public class AddThrowStatementCodeFixProvider : CodeFixProvider
    {
        private const string FixText = "Add 'throw;' statement";
        public override ImmutableArray<string> FixableDiagnosticIds =>  
            ImmutableArray.Create(EmptyCatchBlockAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Create a new block with a list that contains a throw statement.
            var throwStatement = SyntaxFactory.ThrowStatement();

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var token = root.FindToken(diagnosticSpan.Start); // This is catch keyword.

            var catchBlock = token.Parent as CatchClauseSyntax;

            //var blockWithThrow = catchBlock.Block.Statements.Add(throwStatement);
            //var newBlock = SyntaxFactory.Block().WithStatements(blockWithThrow).WithAdditionalAnnotations(Formatter.Annotation);
            var newBlock = catchBlock.Block.AddStatements(throwStatement).WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(catchBlock, catchBlock.WithBlock(newBlock));

            var codeAction = CodeAction.Create(FixText, ct => Task.FromResult(context.Document.WithSyntaxRoot(newRoot)));
            context.RegisterCodeFix(codeAction, diagnostic);
        }

    }
}