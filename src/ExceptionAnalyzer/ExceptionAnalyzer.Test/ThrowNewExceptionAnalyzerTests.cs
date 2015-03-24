using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace ExceptionAnalyzer.Test
{
    [TestClass]
    public class ThrowNewExceptionAnalyzerTests : CodeFixVerifier
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
    class CustomException : Exception {}
   
    class CustomExceptionWithInnerException : Exception
    {
        public CustomExceptionWithInnerException(Exception inner, string message)
            : base(message, inner)
        {}

        public CustomExceptionWithInnerException(string message)
            : base(message)
        {}
    }
    
    class TypeName
    {
        {placeholder}
    }
}";

        [TestMethod]
        public void NoWarningOnThrowExceptionWithInnerException()
        {
            var test =
                TestBase.Replace("{placeholder}", @"
            private readonly Exception ex;
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch(Exception ex) {{on}throw new Exception(""foo"", ex);}
            }");

            Assert.IsFalse(HasWarning(test, ThrowNewExceptionAnalyzer.DiagnosticId));
        }

        [TestMethod]
        public void WarningOnThrowException()
        {
            var test = 
                TestBase.Replace("{placeholder}", @"
            private readonly Exception ex;
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch(Exception ex) {{on}throw new Exception(ex.Message);}
            }");

            AssertHasWarning(test, ThrowNewExceptionAnalyzer.DiagnosticId);
        }

        [TestMethod]
        public void WarningOnThrowExceptionWithMessage()
        {
            var test =
                TestBase.Replace("{placeholder}", @"
            private readonly Exception ex;
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch(Exception ex) {{on}throw new Exception(""foo"");}
            }");

            AssertHasWarning(test, ThrowNewExceptionAnalyzer.DiagnosticId);
        }

        [TestMethod]
        public void WarningOnThrowCustomException()
        {
            var test =
                TestBase.Replace("{placeholder}", @"
            private readonly Exception ex;
            public void Foo()
            {
               try { Console.WriteLine(); }
               catch(Exception ex) {{on}throw new CustomException();}
            }");

            AssertHasWarning(test, ThrowNewExceptionAnalyzer.DiagnosticId);
        }

        [TestMethod]
        public void FixShouldAddExAsInnerException()
        {
            var test =
    TestBase.Replace("{placeholder}", @"
        public void Foo(int n)
        {
            try { Console.WriteLine(); }
            catch(Exception ex)
            {
                throw new Exception(""foo"");
            }
        }");

            var fixtest = TestBase.Replace("{placeholder}", @"
        public void Foo(int n)
        {
            try { Console.WriteLine(); }
            catch(Exception ex)
            {
                throw new Exception(""foo"", ex);
            }
        }");

            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void FixShouldAddExceptionDeclarationAndModifyThrowStatement()
        {
            var test =
    TestBase.Replace("{placeholder}", @"
        public void Foo(int n)
        {
            try { Console.WriteLine(); }
            catch
            {
                throw new Exception(""foo"");
            }
        }");

            var fixtest = TestBase.Replace("{placeholder}", @"
        public void Foo(int n)
        {
            try { Console.WriteLine(); }
            catch (Exception ex)
            {
                throw new Exception(""foo"", ex);
            }
        }");

            VerifyCSharpFix(test, fixtest, null, true);
        }

        [TestMethod]
        public void FixShouldAddExceptionIdentifierAndModifyThrowStatement()
        {
            var test =
    TestBase.Replace("{placeholder}", @"
        public void Foo(int n)
        {
            try { Console.WriteLine(); }
            catch (Exception)
            {
                throw new Exception(""foo"");
            }
        }");

            var fixtest = TestBase.Replace("{placeholder}", @"
        public void Foo(int n)
        {
            try { Console.WriteLine(); }
            catch (Exception ex)
            {
                throw new Exception(""foo"", ex);
            }
        }");

            VerifyCSharpFix(test, fixtest);
        }


        [TestMethod]
        [Ignore]
        // TODO: test is failing! This feature is not implemented in this way!
        public void FixShouldAddExAsInnerExceptionForTheFirstArgument()
        {
            var test =
    TestBase.Replace("{placeholder}", @"
        public void Foo(int n)
        {
            try { Console.WriteLine(); }
            catch(Exception ex)
            {
                throw new CustomExceptionWithInnerException(""foo"");
            }
        }");

            var fixtest = TestBase.Replace("{placeholder}", @"
        public void Foo(int n)
        {
            try { Console.WriteLine(); }
            catch(Exception ex)
            {
                throw new CustomExceptionWithInnerException(ex, ""foo"");
            }
        }");

            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void FixShouldAddFakedTextAndEx()
        {
            var test =
    TestBase.Replace("{placeholder}", @"
        public void Foo(int n)
        {
            try { Console.WriteLine(); }
            catch(Exception ex)
            {
                throw new Exception();
            }
        }");

            var fixtest = TestBase.Replace("{placeholder}", @"
        public void Foo(int n)
        {
            try { Console.WriteLine(); }
            catch(Exception ex)
            {
                throw new Exception("""", ex);
            }
        }");

            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ThrowNewExceptionAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ThrowNewExceptionAnalyzer();
        }
    }
}
