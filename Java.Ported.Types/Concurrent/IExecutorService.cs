using System;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.Java.Types.Concurrent
{
    public interface IExecutorService
    {
        /// <summary>
        /// Submits a value-returning task for execution and returns a Future
        /// representing the pending results of the task.Upon completion,
        /// this task may be taken or polled.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        IFuture<T> submit<T>(FutureTask<T> task);
    }
}
