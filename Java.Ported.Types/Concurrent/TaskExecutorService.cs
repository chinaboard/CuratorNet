using System;
using System.Threading.Tasks;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.Java.Types.Concurrent
{
    public class TaskExecutorService : IExecutorService
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

        public IFuture<T> submit<T>(FutureTask<T> task)
        {
            TaskFactory.StartNew(task.run, task.CancelToken.Token);
            return task;
        }
    }
}
