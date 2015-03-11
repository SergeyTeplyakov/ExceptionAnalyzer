using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExceptionAnalyzer.Utils
{
    internal static class SymbolExtensions
    {
        //private static Lazy<PropertyInfo> _catchPropertyInfo = Lazy.Create(() => typeof(ISymbol))

        public static bool ExceptionFromCatchBlock(this ISymbol symbol)
        {
            return 
                (symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()) is CatchDeclarationSyntax;

            // There is additional interface, called ILocalSymbolInternal
            // that has IsCatch property, but, unfortunately, that interface is internal.
            // Use following code if the trick with DeclaredSyntaxReferences would not work properly!
            // return (bool?)(symbol.GetType().GetRuntimeProperty("IsCatch")?.GetValue(symbol)) == true;
        }
    }
}
