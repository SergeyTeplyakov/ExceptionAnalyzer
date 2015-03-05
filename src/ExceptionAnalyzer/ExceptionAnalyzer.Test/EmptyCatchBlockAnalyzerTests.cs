using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHelper;

namespace ExceptionAnalyzer.Test
{
    [TestClass]
    public class EmptyCatchBlockAnalyzerTests : CodeFixVerifier
    {
        [TestMethod]
        public void TestWarningOnEmptyBlock()
        {
        var test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public static void Foo()
        {
            try { Console.WriteLine(); }
            catch {}
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = EmptyCatchBlockAnalyzer.DiagnosticId,
                Message = "'catch' block is empty. Do you really know what the app state is?",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 11, 13)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public static void Foo()
        {
            try { Console.WriteLine(); }
            catch
            {
                throw;
            }
        }
    }
}";

            VerifyCSharpFix(test, fixtest);

        }

        [TestMethod]
        public void TestWarningOnEmptyBlockThatCatchesException()
        {
        var test = @"
using System;
namespace ConsoleApplication1
{
    class TypeName
    {
        public static void Foo()
        {
            try { Console.WriteLine(); }
            catch(System.Exception) {}
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = EmptyCatchBlockAnalyzer.DiagnosticId,
                Message = "'catch(System.Exception)' block is empty. Do you really know what the app state is?",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 13)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

        var fixtest = @"
using System;
namespace ConsoleApplication1
{
    class TypeName
    {
        public static void Foo()
        {
            try { Console.WriteLine(); }
            catch(System.Exception)
            {
                throw;
            }
        }
    }
}";

            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new EmptyCatchBlockAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new  EmptyCatchBlockAnalyzer();
        }

    }
}
