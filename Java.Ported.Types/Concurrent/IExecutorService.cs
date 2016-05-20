using System;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.Java.Types.Concurrent
{
    public interface IExecutorService
    {
        /**
         * Submits a value-returning task for execution and returns a Future
         * representing the pending results of the task.  Upon completion,
         * this task may be taken or polled.
         *
         * @param task the task to submit
         * @return a future to watch the task
         */
        IFuture<T> submit<T>(Func<T> task) where T : class;

        /**
         * Submits a Runnable task for execution and returns a Future
         * representing that task.  Upon completion, this task may be
         * taken or polled.
         *
         * @param task the task to submit
         * @return a future to watch the task
         */
        IFuture<object> submit(Action task);

        IFuture<T> submit<T>(FutureTask<T> task);
    }
}
