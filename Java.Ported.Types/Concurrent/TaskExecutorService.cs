using System;
using System.Threading.Tasks;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.Java.Types.Concurrent
{
    public class TaskExecutorService : IExecutorService
    {
        private readonly TaskFactory _taskFactory;

        public TaskExecutorService()
        {
            var taskScheduler = TaskScheduler.Current;
            _taskFactory = new TaskFactory(taskScheduler);
        }

        public TaskExecutorService(TaskFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            _taskFactory = factory;
        }

        public IFuture<T> submit<T>(FutureTask<T> task)
        {
            _taskFactory.StartNew(task.run, task.CancelToken.Token);
            return task;
        }
    }
}
