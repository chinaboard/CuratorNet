using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Org.Apache.Java.Types.Concurrent.Atomics;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.Java.Types.Concurrent
{
    public sealed class LimitedTaskExecutorService : IExecutorService
    {
        private const int VolatileReadThreshold = 20;
        private readonly int _maxTasks;
        private readonly AtomicInteger _curTasksCount = new AtomicInteger();
        private readonly ConcurrentQueue<Task> _pendingTaskQueue = new ConcurrentQueue<Task>();
        private readonly TaskFactory _taskFactory;

        private volatile bool _disposed = false;

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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            ThrowIfDisposed();
            _disposed = true;
        }

        public IFuture<T> submit<T>(FutureTask<T> task)
        {
            ThrowIfDisposed();
            Task newTask = CreateTask(task);
            //try start task ASAP:
            //speculativly increment current task count and if we below max task threshold
            //start task right now. Otherwise, add it to queue
            if (_curTasksCount.IncrementAndGet() <= _maxTasks)
            {
                StartTask(newTask);
            }
            else
            {
                // we speculatively increment current task count before
                // and exceed max simultanious tasks => we must decrease 
                // counter back to original value
                _curTasksCount.DecrementAndGet();
                _pendingTaskQueue.Enqueue(newTask);
                if (_curTasksCount.Get() <= 0)// this is linerization point with TaskCompletedHandler method.
                                              // If all currently run task finished between counter atomic 
                                              // increment and task enqueue, then we have to start pending tasks 
                                              // manually because no task completion handlers see last enqueued value.
                                              // less than 0 values not expected, but to prevent some unexpected
                                              // implementation details we support this case
                {
                    ForkPending();
                }
            }
            return task;
        }

        /// <summary>
        /// Method start pending tasks from pending task queue
        /// </summary>
        private void ForkPending()
        {
            ThrowIfDisposed();
            Task task;
            while (_pendingTaskQueue.TryDequeue(out task))
            {
                if (_curTasksCount.IncrementAndGet() <= _maxTasks) //we can fork new task from queue
                {
                    StartTask(task);
                    continue;
                }
                // we speculatively increment current task count before
                // and exceed max simultanious tasks => we must decrease 
                // counter back to original value
                _curTasksCount.DecrementAndGet();
                break;
            }
        }

//        private void TaskConsumer()
//        {
//            //try reduce expensive volatile reads
//            bool disposedLocal = _disposed;
//            int volatileReadThreshold = 0;
//            int nopExecCount = 0;
//            while (!disposedLocal)
//            {
//                Task task;
//                if (!_pendingTaskQueue.TryDequeue(out task))
//                {
//                    nopExecCount++;
//                    if (nopExecCount >= 30)
//                    {
////                        Thread.Sleep();
//                    }
//                    else
//                    {
//                        Thread.Yield();
//                    }
//                    continue;
//                }
//                StartTask(task);
//                if (volatileReadThreshold >= VolatileReadThreshold)
//                {
//                    disposedLocal = _disposed;
//                    volatileReadThreshold = 0;
//                }
//            }
//        }

        private Task CreateTask<T>(FutureTask<T> task)
        {
            return new Task(task.run, task.CancelToken.Token)
                        .ContinueWith(TaskCompletedHandler);
        }

        private void TaskCompletedHandler(Task task)
        {
            _curTasksCount.DecrementAndGet();// this is linerization point with submit() method,
                                             // when it add task to pending queue
            ForkPending();
        }

        private void StartTask(Task task)
        {
            task.Start(_taskFactory.Scheduler);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("");
            }
        }
    }
}
