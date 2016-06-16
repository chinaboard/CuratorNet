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

        /// <summary>
        /// Executes the given command at some time in the future. 
        /// The command may execute in a new thread, in a pooled thread, 
        /// or in the calling thread, at the discretion of the Executor implementation.
        /// </summary>
        /// <param name="command"></param>
        public override void execute(IRunnable command)
        {
            submit(new FutureTask<object>(command));
        }
    }
}
