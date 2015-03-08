using ExceptionAnalyzer.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace ExceptionAnalyzer
{
    [ExportCodeFixProvider("ThrowExAnalyzerCodeFixProvider", LanguageNames.CSharp), Shared]
    public class ThrowExAnalyzerCodeFixProvider : CodeFixProvider
    {
        const string FixText = "Rethrow exception using 'throw;'";

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(ThrowExAnalyzer.DiagnosticId);
            }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();

            var originalThrowStatement = GetThrowStatementFrom(diagnostic, root);
            Contract.Assert(originalThrowStatement != null);

            var originalCatchDeclaration = originalThrowStatement.Ancestors().OfType<CatchClauseSyntax>().First();

            // Fixing "throw ex" to "throw"
            var emptyThrow = SyntaxFactory.ThrowStatement()
                .WithTrailingTrivia(originalThrowStatement.GetTrailingTrivia())
                .WithLeadingTrivia(originalThrowStatement.GetLeadingTrivia());

            var modifiedCatchDeclaration = originalCatchDeclaration.ReplaceNode(originalThrowStatement, emptyThrow);

            // Potential another approach: use CSharpSyntaxRewriter
            // Checking, whether "ex" was used only in "throw"
            bool onlyUsedByThrow = await ExceptionDeclarationCouldBeRemoved(context, root, originalThrowStatement);
            if (onlyUsedByThrow)
            {
                // Fixing "catch(Exception ex)" to "catch(Exception)"
                // TODO: don't know another way to remove the identifier.

                // NormalizeWhitespace will change "catch(Exception )" to "catch(Exception)"
                var newCatchDeclaration = SyntaxFactory.CatchDeclaration(originalCatchDeclaration.Declaration.Type).NormalizeWhitespace();

                // We should keep trivia after old catch block, like "catch(Exception ex) // trivia!!!"
                var closedParenWithTrivia = newCatchDeclaration.CloseParenToken.WithTrailingTrivia(modifiedCatchDeclaration.Declaration.CloseParenToken.TrailingTrivia);
                
                modifiedCatchDeclaration = modifiedCatchDeclaration.WithDeclaration(newCatchDeclaration.WithCloseParenToken(closedParenWithTrivia));
            }

            // Replacing the tree
            var newRoot = root.ReplaceNode(originalCatchDeclaration, modifiedCatchDeclaration);

            var codeAction = CodeAction.Create(FixText, token => Task.FromResult(context.Document.WithSyntaxRoot(newRoot)));
            context.RegisterCodeFix(codeAction, diagnostic);
        }

        private static async Task<bool> ExceptionDeclarationCouldBeRemoved(CodeFixContext context, SyntaxNode root, ThrowStatementSyntax originalThrowStatement)
        {
            // If "ex" from "throw ex" was the only reference to "ex", then additional modification should be made
            // "catch(Exception ex)" should be replaced by "catch(Exception)"

            var throwExIdentifier = originalThrowStatement.Expression.As(x => x as IdentifierNameSyntax);

            Contract.Assert(throwExIdentifier != null);

            var model = await context.Document.GetSemanticModelAsync();
            var solution = context.Document.Project.Solution;
            var symbol = model.GetSymbolInfo(throwExIdentifier);

            Contract.Assert(symbol.Symbol != null);

            // Not sure this is a good idea!
            // TODO: talk to nikov about it! If there is an optimization for locals than everything should be fine!
            // Otherwise - not!
            // Searching within one document should be fast. Still need to check!

            var references = await SymbolFinder.FindReferencesAsync(symbol.Symbol, solution, ImmutableHashSet.Create(context.Document));
            var locations = references.SelectMany(x => x.Locations).ToArray();
            var numberOfUsages =
                references
                .SelectMany(x => x.Locations)
                .Select(x => root.FindToken(x.Location.SourceSpan.Start))
                .Count(token => token.Parent.Parent is ThrowStatementSyntax); // TODO: code duplication with GetThrowStatementFrom!

            // "ex" in the "Exception ex" could be removed only if there is no any other usages. Otherwise the fix will fail.
            // Consider following case:
            // There is two usages of the "ex" in two "throw ex" statemetns.
            // Two different fixes would be run for both warnings.
            // The first fix will change the code to "catch(Exception) {throw ex; throw;}" witch is not a valid C# program
            return numberOfUsages == 1 && locations.Length == 1;
        }

        [Pure]
        private ThrowStatementSyntax GetThrowStatementFrom(Diagnostic d, SyntaxNode root)
        {
            var diagnosticSpan = d.Location.SourceSpan;

            // Diagnostics reports warning on the "ex" of the "throw ex" statement.
            // I.e. it points to the identifier.
            var token = root.FindToken(diagnosticSpan.Start); // This is catch keyword.

            // To find original throw statement, we should go up on two levels
            // TODO: find a generic traversal algorithm!
            return token.Parent.Parent as ThrowStatementSyntax;
        }
    }
}