using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExceptionAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EmptyCatchBlockAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "EmptyCatchBlock";
        // TODO: extract all messages somewhere to be able to add errogant messages
        internal const string Title = "Empty catch block considered very harmful!";
        internal const string MessageFormat = "'{0}' is empty, app could be unknowingly missing exceptions";
        internal const string Category = "CodeSmell";

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

            // If catch is present we must have a block, so check if block empty?
            if (catchBlock?.Block.Statements.Count == 0)
            {
                // Block is empty, create and report diagnostic warning.
                var diagnostic = Diagnostic.Create(Rule,
                  catchBlock.CatchKeyword.GetLocation(), "Catch block");
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
