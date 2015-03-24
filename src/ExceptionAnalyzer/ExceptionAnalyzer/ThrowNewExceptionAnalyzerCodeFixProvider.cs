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
    public class ThrowNewExceptionAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string FixText = "Add catched exception as InnerException";

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(ThrowNewExceptionAnalyzer.DiagnosticId);
            }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Fix looks up for a constructor that accepts string argument
            // and exception object.

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();

            var originalThrowStatement = GetThrowStatementFrom(diagnostic, root);

            var creationExpression = originalThrowStatement.Expression as ObjectCreationExpressionSyntax;
            Contract.Assert(creationExpression != null);

            var semanticModel = await context.Document.GetSemanticModelAsync();

            // Looking for symbol for thrown excpetion (i.e. Exception type for throw new Exception())
            var exceptionTypeSymbol = semanticModel.GetSymbolInfo(creationExpression.Type).Symbol as INamedTypeSymbol;
            if (exceptionTypeSymbol == null)
            {
                // Can't resolve excpetion type..
                return;
            }

            // Naive implementation: just looking for string, exception constructor.
            bool hasAppropriateConstructor =
                exceptionTypeSymbol.Constructors
                .Any(c => c.Parameters.Length == 2 && 
                          c.Parameters[0].Type.Name.ToLower().Contains("string") && 
                          c.Parameters[1].Type.Name.ToLower().Contains("exception"));
            
            if (!hasAppropriateConstructor)
            {
                // Can't make the fix! throwing exception does not has appropriate constructor!
                return;
            }

            // Adding ex identifier if required
            var catchClause = originalThrowStatement.AncestorsAndSelf().OfType<CatchClauseSyntax>().First();

            var originalCatchClause = catchClause;

            SyntaxToken identifier;
            
            if (catchClause.Declaration == null || catchClause.Declaration.Identifier.Kind() == SyntaxKind.None)
            {
                identifier = SyntaxFactory.Identifier("ex");
            }
            else
            {
                identifier = catchClause.Declaration.Identifier;
            }

            ArgumentSyntax additionalArgument = SyntaxFactory.Argument(SyntaxFactory.IdentifierName(identifier));

            ObjectCreationExpressionSyntax newCreationExpression;

            var arguments = creationExpression.ArgumentList;

            // Add fake "message" argument if needed
            if (creationExpression.ArgumentList.Arguments.Count == 0)
            {
                // Using just string.Empty
                arguments = 
                    arguments.AddArguments(
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(""))));
            }

            newCreationExpression =
                creationExpression.WithArgumentList(
                    arguments.AddArguments(additionalArgument));

            catchClause = catchClause.ReplaceNode(creationExpression, newCreationExpression);

            if (catchClause.Declaration == null)
            {
                // this is "catch {}" block
                catchClause = await CatchUtils.WitchExceptionDeclarationAsync(catchClause, context.Document);
            }

            if (catchClause.Declaration.Identifier.Kind() == SyntaxKind.None)
            {
                // This is catch(Exception)
                catchClause = catchClause.WithDeclaration(catchClause.Declaration.WithIdentifier(identifier));
            }
            
            var newRoot = root.ReplaceNode(originalCatchClause, catchClause);

            var codeAction = CodeAction.Create(FixText, 
                token => Task.FromResult(context.Document.WithSyntaxRoot(newRoot)));

            context.RegisterCodeFix(codeAction, diagnostic);
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
            return token.Parent.AncestorsAndSelf().OfType<ThrowStatementSyntax>().First();
        }
    }
}