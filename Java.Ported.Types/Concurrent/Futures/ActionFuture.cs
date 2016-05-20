using System;
using System.Threading.Tasks;

namespace Org.Apache.Java.Types.Concurrent.Futures
{
    internal sealed class ActionFuture : IFuture<object>
    {
        private readonly Task _task;

        public ActionFuture(Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }
            _task = task;
        }

        public bool cancel()
        {
            return false;
        }

        public bool isCancelled()
        {
            return _task.IsCanceled;
        }

        public bool isDone()
        {
            return _task.IsCompleted;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public object get()
        {
            _task.Wait();
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public object get(int timeoutMs)
        {
            _task.Wait(timeoutMs);
            return null;
        }
    }
}
