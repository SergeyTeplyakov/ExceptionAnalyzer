using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ExceptionAnalyzer
{
    /// <summary>
    /// Detects emtpy generic catch blocks like `catch{}` or `catch(Exception){}`.
    /// </summary>
    /// <remarks>
    /// TODO: right now, <see cref="EmptyCatchBlockAnalyzer"/> is a subset of <see cref="GenericCatchBlockAnalyzer"/>. Revisit this approach later!
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EmptyCatchBlockAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "Empty catch block";
        // TODO: extract all messages somewhere to be able to add errogant messages
        internal const string Title = "Empty catch block considered harmful!";
        internal const string MessageFormat = "{0} block is empty. Do you really know what the app state is?";
        internal const string Category = "CodeSmell";

        // It seems that System.ApplicationException is absent in Portable apps
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.CatchClause);
        }

        // Called when Roslyn encounters a catch clause.
        private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            // Type cast to what we know.
            var catchBlock = context.Node as CatchClauseSyntax;
            if (catchBlock == null)
            {
                return;
            }

            if (catchBlock.Block.Statements.Count != 0)
            {
                return;
            }

            if (catchBlock.Declaration == null || CatchIsTooGeneric(context, catchBlock.Declaration))
            {
                var type = catchBlock.Declaration?.ToString() ?? "";
                string catchClause = $"'catch{type}'";
                // Block is empty, create and report diagnostic warning.
                var diagnostic = Diagnostic.Create(Rule, catchBlock.CatchKeyword.GetLocation(), catchClause);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool CatchIsTooGeneric(SyntaxNodeAnalysisContext context, CatchDeclarationSyntax declaration)
        {
            var symbol = context.SemanticModel.GetSymbolInfo(declaration.Type);
            if (symbol.Symbol == null)
            {
                return false;
            }

            var exception = context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Exception).FullName);
            return symbol.Symbol == exception;
        }
    }
}
