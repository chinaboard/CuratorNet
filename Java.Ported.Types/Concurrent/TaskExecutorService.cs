using System;
using System.Threading.Tasks;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.Java.Types.Concurrent
{
    public class TaskExecutorService : ExecutorServiceBase
    {
        protected readonly TaskFactory TaskFactory;

        public TaskExecutorService()
        {
            var taskScheduler = TaskScheduler.Current;
            TaskFactory = new TaskFactory(taskScheduler);
        }

        public TaskExecutorService(TaskFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            TaskFactory = factory;
        }

        public override IFuture<T> submit<T>(FutureTask<T> task)
        {
            ThrowIfDisposed();
            TaskFactory.StartNew(task.run, task.CancelToken.Token);
            return task;
        }
    }
}
