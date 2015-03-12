using System;
using System.Diagnostics.Contracts;

namespace ExceptionAnalyzer.Utils
{
    internal static class Lazy
    {
        public static Lazy<T> Create<T>(Func<T> valueFactory)
        {
            Contract.Requires(valueFactory != null);

            return new Lazy<T>(valueFactory);
        }
    }
}
