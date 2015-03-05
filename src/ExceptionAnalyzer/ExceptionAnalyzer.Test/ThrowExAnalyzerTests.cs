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
    public class ThrowExAnalyzerTests : CodeFixVerifier
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
        public void NoWarningWhenThrowingInstanceVariable()
        {
            var test = 
                TestBase.Replace("{placeholder}", @"
            private readonly Exception ex;
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch(Exception ex) {throw this.ex;}
            }");

            Assert.IsFalse(HasWarning(test));
        }

        [TestMethod]
        public void NoWarningOnEmptyCatch()
        {
            var test =
                TestBase.Replace("{placeholder}", @"
            private readonly Exception ex;
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch {throw;}
            }");

            Assert.IsTrue(HasWarning(test));
        }

        [TestMethod]
        public void WarningOnThrowWithStaticEx()
        {
            var test =
                TestBase.Replace("{placeholder}", @"
            private readonly static Exception ex;
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch(Exception ex) {throw ex;}
            }");

            Assert.IsTrue(HasWarning(test));
        }

        [TestMethod]
        public void WarningOnThrowEx()
        {
            var test =
                TestBase.Replace("{placeholder}", @"
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch(Exception ex) {throw ex;}
            }");

            Assert.IsTrue(HasWarning(test));
        }

        [TestMethod]
        public void TestTwoWarnings()
        {
            var test =
                TestBase.Replace("{placeholder}", @"
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch(Exception ex)
               {
                 if (ex.Message.Length == 5) throw ex;
                 throw ex;
               }
            }");

            Assert.AreEqual(2, GetSortedDiagnostics(test).Length);
        }

        [TestMethod]
        public void TestWarningWithLocationAndFix()
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
            catch(Exception ex) {Console.WriteLine(ex); throw ex;}
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = ThrowExAnalyzer.DiagnosticId,
                Message = ThrowExAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 16, 63)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = test.Replace("catch(Exception ex) {Console.WriteLine(ex); throw ex;}", @"catch(Exception ex) {Console.WriteLine(ex); throw; }");

            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void FixShouldRemoveExIfNeverUsed()
        {
            var test =
    TestBase.Replace("{placeholder}", @"
        public void Foo(int n)
        {
            try { Console.WriteLine(); }
            catch(Exception ex)
            {
                if (n == 1) throw ex;
                throw ex;
            }
        }");

            Assert.AreEqual(2, GetSortedDiagnostics(test).Length);
            // TODO: currently the fix is breaking the layout. 
            var fixtest = TestBase.Replace("{placeholder}", @"
        public void Foo(int n)
        {
            try { Console.WriteLine(); }
            catch(Exception)
            {
                if (n == 1)
                    throw;
                throw;
            }
        }");

            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void FixShouldPreserveMoreComplexTrivia()
        {
            var test =
    TestBase.Replace("{placeholder}", @"
        public void Foo(int n)
        {
            try { Console.WriteLine(); }
            catch(Exception ex) // some comment!!!
            {
                if (n == 1) throw ex;
                throw ex;
            }
        }");

            Assert.AreEqual(2, GetSortedDiagnostics(test).Length);
            // TODO: currently the fix is breaking the layout. 
            var fixtest = TestBase.Replace("{placeholder}", @"
        public void Foo(int n)
        {
            try { Console.WriteLine(); }
            catch(Exception) // some comment!!!
            {
                if (n == 1)
                    throw;
                throw;
            }
        }");

            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ThrowExAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new  ThrowExAnalyzer();
        }
    }
}
