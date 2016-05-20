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

        public IFuture<T> submit<T>(Func<T> task) where T : class
        {
            Task<T> runnedTask = _taskFactory.StartNew(task);
            return new FutureTask<T>(runnedTask);
        }

        public IFuture<object> submit(Action task)
        {
            Task runnedTask = _taskFactory.StartNew(task);
            return new ActionFuture(runnedTask);
        }

        public IFuture<T> submit<T>(FutureTask<T> task)
        {
            task.run();
            return task;
        }
    }
}
