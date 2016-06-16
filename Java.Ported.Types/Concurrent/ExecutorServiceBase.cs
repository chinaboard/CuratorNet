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

        /// <summary>
        /// Executes the given command at some time in the future. 
        /// The command may execute in a new thread, in a pooled thread, 
        /// or in the calling thread, at the discretion of the Executor implementation.
        /// </summary>
        /// <param name="command"></param>
        public abstract void execute(IRunnable command);

        protected void ThrowIfDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("");
            }
        }
    }
}
