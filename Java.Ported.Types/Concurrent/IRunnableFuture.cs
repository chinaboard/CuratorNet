using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.Java.Types.Concurrent
{
    /// <summary>
    /// A Future that is Runnable. Successful execution of
    /// the "run" method causes completion of the Future and allows access to its results.
    /// </summary>
    /// <typeparam name="T">The result type returned by this Future's "get" method</typeparam>
    public interface IRunnableFuture<T> : IRunnable, IFuture<T> 
    {
        /**
         * Sets this Future to the result of its computation
         * unless it has been cancelled.
         */
        void run();
    }

}
