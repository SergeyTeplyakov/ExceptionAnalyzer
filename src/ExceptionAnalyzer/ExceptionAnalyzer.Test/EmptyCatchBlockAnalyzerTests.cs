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
        //[TestMethod]
        public void TestWarningOnEmptyBlock()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

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
                Message = "'Catch block' is empty, app could be unknowingly missing exceptions",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 16, 17)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = test.Replace("catch {}",
@"catch
                {
                    throw;
                }");

            VerifyCSharpFix(test, fixtest);

        }

        [TestMethod]
        public void TestWarningOnEmptyBlockThatCatchesException()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

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
                Message = "'Catch block' is empty, app could be unknowingly missing exceptions",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 16, 17)
                        }
            };
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
