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
    public class GenericCatchBlockAnalyzerTests : CodeFixVerifier
    {
        private const string TestBase = @"
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
            {placeholder}
        }
    }";

        [TestMethod]
        public void TestWarningOnEmptyCatchBlock()
        {
            var test = TestBase.Replace("{placeholder}", @"
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch {Console.WriteLine();}
            }");

            Assert.IsTrue(HasWarning(test));
        }

        [TestMethod]
        public void TestWarningOnEmptyCatchBlockWithReturn()
        {
            var test = TestBase.Replace("{placeholder}", @"
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch {Console.WriteLine(); return;}
            }");

            Assert.IsTrue(HasWarning(test));
        }

        [TestMethod]
        public void TestWarningOnEmptyCatchBlockWithConditionalReturn()
        {
            var test = TestBase.Replace("{placeholder}", @"
            public void Foo(int n)
            {
               try { Console.WriteLine(); }
               catch {Console.WriteLine(); if (n == 42) return; throw;}
            }");

            Assert.IsTrue(HasWarning(test));
        }

        [TestMethod]
        public void TestNoWarningOnEmptyCatchBlockWithThrow()
        {
            var test = TestBase.Replace("{placeholder}", @"
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch {Console.WriteLine();throw;}
            }");

            Assert.IsFalse(HasWarning(test));
        }

        [TestMethod]
        public void TestWarningWithFix()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public static void Foo()
        {
            try
            {
                Console.WriteLine();
            }
            catch // something!
            {
                Console.WriteLine();
            }
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = GenericCatchBlockAnalyzer.DiagnosticId,
                Message = "Catching everything considered harmful!\r\n Are you not curious at all about exception type?",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 14, 13)
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
            try
            {
                Console.WriteLine();
            }
            catch (Exception ex) // something!
            {
                Console.WriteLine();
            }
        }
    }
}";


            VerifyCSharpFix(test, fixtest, allowNewCompilerDiagnostics: true);
        }

        [TestMethod]
        public void TestFixWithoutUsingStatement()
        {
            var test = @"
//using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public static void Foo()
        {
            try
            {
                Console.WriteLine();
            }
            catch
            {
                Console.WriteLine();
            }
        }
    }
}";

            Assert.IsTrue(HasWarning(test));

            var fixtest = @"
//using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public static void Foo()
        {
            try
            {
                Console.WriteLine();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine();
            }
        }
    }
}";

            VerifyCSharpFix(test, fixtest, allowNewCompilerDiagnostics: true);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new GenericCatchBlockCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new GenericCatchBlockAnalyzer();
        }
    }
}
