using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExceptionAnalyzer.Utils
{
    public static class Monadic
    {
        public static U As<T, U>(this T t, Func<T, U> convertor) where U : class
        {
            return convertor(t);
        }
        //public static U As<U>(this object t) where U : class
        //{
        //    return t as U;
        //}

        //public static U As<T, U>(this T t) where T : U where U : class
        //{

        //}
    }

    class MyClass<T>
    {
        public MyClass(T value)
        {
            _value = value;
        }

        private readonly T _value;
        public T Value => _value;

        public static implicit operator T(MyClass<T> myClassToConvert)
        {
            return myClassToConvert.Value;
        }

        public U As<U>() where U : class
        {
            return this as U;
        }
        public static implicit operator MyClass<T>(T myClassToConvert)
        {
            return new MyClass<T>(myClassToConvert);
        }
    }
}
