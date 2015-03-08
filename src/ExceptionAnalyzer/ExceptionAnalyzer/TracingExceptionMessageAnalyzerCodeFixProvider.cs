using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExceptionAnalyzer
{
    [ExportCodeFixProvider("ThrowExAnalyzerCodeFixProvider", LanguageNames.CSharp), Shared]
    public class TracingExceptionMessageAnalyzerCodeFixProvider : CodeFixProvider
    {
        const string FixTextFormat = "Observe exception with '{0}'";

        public override ImmutableArray<string> FixableDiagnosticIds => 
            ImmutableArray.Create(TracingExceptionMessageAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();

            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var messageToken = root.FindToken(diagnosticSpan.Start); // Message identifier from the ex.Message

            var messageUsage = messageToken.Parent.AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().First();

            // Creating 'ex.ToString()'
            var toStringUsage = SyntaxFactory.InvocationExpression(
                messageUsage.WithName(SyntaxFactory.IdentifierName("ToString")));
            
            // Replacing the tree
            var newRoot = root.ReplaceNode(messageUsage, toStringUsage);

            var fixText = string.Format(FixTextFormat, toStringUsage.GetText());

            var codeAction = CodeAction.Create(fixText, token => Task.FromResult(context.Document.WithSyntaxRoot(newRoot)));
            context.RegisterCodeFix(codeAction, diagnostic);
        }
    }
}