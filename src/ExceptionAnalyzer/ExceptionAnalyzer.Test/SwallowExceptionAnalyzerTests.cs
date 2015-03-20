using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using TestHelper;

namespace ExceptionAnalyzer.Test
{
    [TestClass]
    public class SwallowExceptionAnalyzerTests : CodeFixVerifier
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
        public void TestWarningOnGoTo()
        {
            var test = TestBase.Replace("{placeholder}", @"
        public void Foo()
        {
            try
            {

            }
            catch(Exception)
            {
                {on}goto lbl;
            }

            lbl:
            Console.WriteLine(""Hehe"");
        }");

            AssertHasWarning(test, SwallowExceptionAnalyzer.DiagnosticId);
        }

        [TestMethod]
        public void TestWarningOnReturn()
        {
            var test = TestBase.Replace("{placeholder}", @"
        public int Foo(int n)
        {
            try
            {

            }
            catch(Exception)
            {
                if (n == 42)
                    {on}return 42;
                throw;
            }
            return 42;
        }");

            AssertHasWarning(test, SwallowExceptionAnalyzer.DiagnosticId);

            var diagnostic = GetSortedDiagnostics(test).First();
            Assert.AreEqual(
                "Exit point 'return 42;' swallows an exception!\r\nConsider throwing an exception instead.",
                diagnostic.GetMessage());
        }

        [TestMethod]
        public void TestNoWarningOnReturnIfNotReachable()
        {
            var test = TestBase.Replace("{placeholder}", @"
        public int Foo(int n)
        {
            try
            {}
            catch(Exception)
            {
                throw;
                return 42;
            }
            return 42;
        }");

            var count = GetSortedDiagnostics(test).Count(a => a.Id == SwallowExceptionAnalyzer.DiagnosticId);
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public void TestTwoWarningOnReturn()
        {
            var test = TestBase.Replace("{placeholder}", @"
        public void Foo(int n)
        {
            try
            {

            }
            catch(Exception)
            {
                if (n == 42)
                    {on}return;
                {on}return;
            }
        }");

            AssertHasWarning(test, SwallowExceptionAnalyzer.DiagnosticId);
        }


        [TestMethod]
        public void TestWarningOnEndBlockIfReachable()
        {
            var test = TestBase.Replace("{placeholder}", @"
        public void Foo()
        {
            try
            {

            }
            catch(Exception)
            {
            {on}}
        }");

            AssertHasWarning(test, SwallowExceptionAnalyzer.DiagnosticId);
        }


        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return null;
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SwallowExceptionAnalyzer();
        }

    }
}
