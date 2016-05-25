using System;

namespace Org.Apache.Java.Types.Concurrent
{
    public static class CallableUtils
    {
        internal class CallableFromFunc<T> : ICallable<T>
        {
            private readonly Func<T> _func;

            /// <summary>
            /// Initializes a new instance of the <see cref="T:System.Object"/> class.
            /// </summary>
            public CallableFromFunc(Func<T> func)
            {
                if (func == null)
                {
                    throw new ArgumentNullException(nameof(func));
                }
                _func = func;
            }

            /// <summary>
            /// Computes a result, or throws an exception if unable to do so.
            /// </summary>
            /// <returns>computed result</returns>
            /// <exception cref="Exception">if unable to compute a result</exception>
            public T call()
            {
                return _func();
            }
        }

        public static ICallable<T> FromFunc<T>(Func<T> func)
        {
            return new CallableFromFunc<T>(func);
        } 
    }
}
