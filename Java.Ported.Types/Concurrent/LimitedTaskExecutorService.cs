using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Org.Apache.Java.Types.Concurrent.Atomics;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.Java.Types.Concurrent
{
    public sealed class LimitedTaskExecutorService : IExecutorService
    {
        private readonly int _maxTasks;
        private readonly AtomicInteger _curTasksCount = new AtomicInteger();
        private readonly ConcurrentQueue<Task> _pendingTaskQueue = new ConcurrentQueue<Task>();
        private readonly TaskFactory _taskFactory;

        /// <exception cref="ArgumentException"></exception>
        public LimitedTaskExecutorService(int maxTasks)
            : this(maxTasks, new TaskFactory(TaskScheduler.Current)) { }

        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"><paramref name=""/> is <see langword="null" />.</exception>
        public LimitedTaskExecutorService(int maxTasks, TaskFactory factory)
        {
            if (maxTasks <= 0)
            {
                throw new ArgumentException(nameof(maxTasks));
            }
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            _maxTasks = maxTasks;
            _taskFactory = factory;
        }

        public IFuture<T> submit<T>(FutureTask<T> task)
        {
            Task newTask = new Task(task.run, task.CancelToken.Token);
            if (_curTasksCount.CompareAndSet() _curTasksCount < _maxTasks)
            {
                
            }
            newTask.Start(_taskFactory.Scheduler);
            Task newTask = _taskFactory.StartNew(task.run, task.CancelToken.Token)
                                      .ContinueWith(t =>
                                      {
                                          _pendingTaskQueue.TryDequeue()
                                      });
            return task;
        }
    }
}
