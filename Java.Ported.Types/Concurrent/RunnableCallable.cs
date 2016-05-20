using System;

namespace Org.Apache.Java.Types.Concurrent
{
    internal class RunnableCallable<T> : ICallable<T>
    {
        private readonly IRunnable _runnable;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public RunnableCallable(IRunnable runnable)
        {
            if (runnable == null)
            {
                throw new ArgumentNullException(nameof(runnable));
            }
            _runnable = runnable;
        }

        /// <summary>
        /// Computes a result, or throws an exception if unable to do so.
        /// </summary>
        /// <returns>computed result</returns>
        /// <exception cref="Exception">if unable to compute a result</exception>
        public T call()
        {
            _runnable.run();
            return default(T);
        }
    }
}
