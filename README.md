# Roslyn-based Exception Analyzer

Proper exception handling is a common issue in many projects. There is several common code smells but most of them would be obvious for everyone with this analyzer.

This analyzer catches following issues.

## New excpetion should have original exception as InnerException (v.1.0.5)

Catch block could throw another exception that would be more meaningful for the clients. But in this case original exception should not be forgotten.
Tool will warn about this and provide a fix that will add original excpetion to `InnerException` if possible:

![Image1](https://github.com/SergeyTeplyakov/ExceptionAnalyzer/raw/master/docs/Images/ThrowNewException.gif)

## Empty catch block considered harmful!

Application code should not swallow all exceptions using generic catch blocks like `catch {}` or `catch(Exception){}`.
![Image1](https://github.com/SergeyTeplyakov/ExceptionAnalyzer/raw/master/docs/Images/GenericCatch.gif)

## Swallow exceptions considered harmful
Empty catch blocks are dangerous, but even when the `catch` block is not empty it still could swallow exceptions. 
`catch` blocks without `throw;` statement are harmful, because they swallow all exceptions (even non-CLS-compliant) without any chances for proper handling (even tracing is impossible).

This analyzer will warn about such kind of issue and will provide a fix that will add `throw` statement at the bottom of `catch` block.

![Image2](https://raw.githubusercontent.com/SergeyTeplyakov/ExceptionAnalyzer/master/docs/Images/CatchEverything.png)

## Catch block swallows an exception

There is another warning related to swallowed exceptions.

Even non-empty catch block could rethrow exceptions in some cases and swallow them in another. This analyzer will warn for every code path in the catch block that swallows generic exceptions.
There is no fix for such kind of issues yet (replace `return` to `throw`?) but even in this case you'll see that you're hiding exceptions in some cases.

![Image3](https://github.com/SergeyTeplyakov/ExceptionAnalyzer/raw/master/docs/Images/ControlFlowAnalysis.jpg)

## Rethrow exception properly

Incorrect exception propagation is very common. Difference between `throw ex` and `throw` in the `catch(Exception ex)` block is subtle but yet important.
First statement will rethrow original exception but will override the stack trace. `throw` statement on the other hand will propagate original excpetion as is.

![Image4](https://github.com/SergeyTeplyakov/ExceptionAnalyzer/raw/master/docs/Images/ThrowExFix.gif)

## Tracing `ex.Message` considered harmful

Exception object is very complex and could form tree-like structure with `InnerException`s. Unfortunately, there is a planty of code in the wild that uses absolutely insane way for tracing exception and store only `Message` property.
Everytime developer stores only `ex.Message` in log-file God kills a kitty! You should save cats and save important information about exceptions!

This analyzer will warn if you just store `Message` and observe exceptions inappropriately.

![Image5](https://github.com/SergeyTeplyakov/ExceptionAnalyzer/raw/master/docs/Images/ExObserver.gif)