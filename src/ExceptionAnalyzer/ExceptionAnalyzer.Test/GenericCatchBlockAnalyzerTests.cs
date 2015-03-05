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

            Assert.IsFalse(HasWarning(test));
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

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return null;
            //return new GenericCatchBlockAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new GenericCatchBlockAnalyzer();
        }
    }
}
