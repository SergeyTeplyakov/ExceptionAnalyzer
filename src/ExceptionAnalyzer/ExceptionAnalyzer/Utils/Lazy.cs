using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
