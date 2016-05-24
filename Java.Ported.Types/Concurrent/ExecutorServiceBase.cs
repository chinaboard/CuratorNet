using System;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.Java.Types
{
    public abstract class ExecutorServiceBase : IExecutorService
    {
        protected volatile bool Disposed;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, 
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            ThrowIfDisposed();
            Disposed = true;
        }

        /// <summary>
        /// Submits a value-returning task for execution and returns a Future
        /// representing the pending results of the task.Upon completion,
        /// this task may be taken or polled.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        public abstract IFuture<T> submit<T>(FutureTask<T> task);

        protected void ThrowIfDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("");
            }
        }
    }
}
