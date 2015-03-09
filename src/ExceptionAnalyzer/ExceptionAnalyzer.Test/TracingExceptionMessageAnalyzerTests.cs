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
    public class TracingExceptionMessageAnalyzerTests : CodeFixVerifier
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
        public void WarningForWritingExMessage()
        {
            var test = 
                TestBase.Replace("{placeholder}", @"
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch(Exception ex) {Console.WriteLine(ex.Message); throw;}
            }");

            Assert.IsTrue(HasWarning(test));
        }

        [TestMethod]
        public void NoWarningForWritingExMessage()
        {
            var test =
                TestBase.Replace("{placeholder}", @"
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch(Exception ex) {Console.WriteLine(ex.Message); Console.WriteLine(ex); Console.WriteLine(ex.InnerException); throw;}
            }");

            Assert.IsFalse(HasWarning(test));
        }

        [TestMethod]
        public void NoWarningOnWritingFullException()
        {
            var test =
                TestBase.Replace("{placeholder}", @"
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch(Exception ex) {Console.WriteLine(ex); throw;}
            }");

            Assert.IsFalse(HasWarning(test));
        }

        [TestMethod]
        public void NoWarningWhenMessageUsedButWholeExceptionWasSaved()
        {
            var test =
                TestBase.Replace("{placeholder}", @"
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch(Exception ex) {Console.WriteLine(ex.Message); Console.WriteLine(ex); throw;}
            }");

            Assert.IsFalse(HasWarning(test));
        }

        [TestMethod]
        public void TestWithMessageAndPosition()
        {
            var test =
                TestBase.Replace("{placeholder}", @"
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch(Exception ex) {Console.WriteLine(ex.{on}Message); throw;}
            }");

            AssertHasWarning(test, TracingExceptionMessageAnalyzer.DiagnosticId);

            var diagnostic = GetSortedDiagnostics(test).Single();
            Assert.AreEqual("'ex.Message' contains a small portion of useful information. Observe whole exception instead!",
                diagnostic.GetMessage());
        }

        [TestMethod]
        public void TestFix()
        {
            var test =
    TestBase.Replace("{placeholder}", @"
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch(Exception ex) {Console.WriteLine(ex.Message); throw;}
            }");


            var fixtest =
    TestBase.Replace("{placeholder}", @"
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch(Exception ex) {Console.WriteLine(ex.ToString()); throw;}
            }");

            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new TracingExceptionMessageAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new TracingExceptionMessageAnalyzer();
        }
    }
}
