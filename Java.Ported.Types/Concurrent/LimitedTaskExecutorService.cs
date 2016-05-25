using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Org.Apache.Java.Types.Concurrent.Atomics;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.Java.Types.Concurrent
{
    public sealed class LimitedTaskExecutorService : ExecutorServiceBase, IScheduledExecutorService
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

        public override IFuture<T> submit<T>(FutureTask<T> task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }
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
                _pendingTaskQueue.Enqueue(newTask);
                // we speculatively increment current task count before
                // and exceed max simultanious tasks => we must decrease 
                // counter back to original value
                int currentTasks = _curTasksCount.DecrementAndGet();// this is linerization point in concurrent execution 
                                                                    // with TaskCompletedHandler method. This must follow after
                                                                    // enqueuing of new task.
                if (currentTasks < _maxTasks)   // If all currently run task finished between counter atomic 
                                                // increment and task enqueue, then we have to start pending tasks 
                                                // manually because no task completion handlers see last enqueued value.
                                                // Also we have to consider the case when current run task count less 
                                                // then it maximum value and we have chance to run more tasks 
                                                // right now(this case help to run as more task as possible).
                                                // Less than 0 values not expected, but to prevent some unusual
                                                // implementation details we support this case.
                {
                    TryForkPending();
                }
            }
            return task;
        }

        /// <summary>
        /// Method start pending tasks from pending task queue
        /// </summary>
        private void TryForkPending()
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
                //we take task from queue but fail to run it => return it to pending queue
                _pendingTaskQueue.Enqueue(task);
                break;
            }
        }

        private Task CreateTask<T>(FutureTask<T> task)
        {
            Task newTask = new Task(task.run, task.CancelToken.Token);
            newTask.ContinueWith(TaskCompletedHandler);
            return newTask;
        }

        private void TaskCompletedHandler(Task task)
        {
            _curTasksCount.DecrementAndGet();// this is linerization point with submit() method,
                                             // when it add task to pending queue
            TryForkPending();
        }

        private void StartTask(Task task)
        {
            task.Start(_taskFactory.Scheduler);
        }

        public IFuture<object> schedule(FutureTask<object> command, int delayMs)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }
            if (delayMs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(delayMs));
            }
            var envelopedTask = new FutureTask<object>(new ScheduledRunnable(command, delayMs));
            submit(envelopedTask);
            return envelopedTask;
        }

        public IFuture<object> scheduleWithFixedDelay(FutureTask<object> command, 
                                                        int initialDelayMs, 
                                                        int delayMs)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }
            if (delayMs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(delayMs));
            }
            if (initialDelayMs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialDelayMs));
            }
            var runnable = new RepeatableScheduledRunnable(command, initialDelayMs,delayMs);
            var envelopedTask = new FutureTask<object>(runnable);
            submit(envelopedTask);
            return envelopedTask;
        }

        private class ScheduledRunnable : IRunnable
        {
            private readonly FutureTask<object> _task;
            private readonly int _delayMs;

            internal ScheduledRunnable(FutureTask<object> task, int delayMs)
            {
                _task = task;
                _delayMs = delayMs;
            }

            public void run()
            {
                Thread.Sleep(_delayMs);
                _task.run();
            }
        }

        private class RepeatableScheduledRunnable : IRunnable
        {
            private readonly FutureTask<object> _task;
            private readonly int _initialDelayMs;
            private readonly int _delayMs;

            internal RepeatableScheduledRunnable(FutureTask<object> task, 
                                                    int initialDelayMs, 
                                                    int delayMs)
            {
                _task = task;
                _initialDelayMs = initialDelayMs;
                _delayMs = delayMs;
            }

            public void run()
            {
                Thread.Sleep(_initialDelayMs);
                while (!_task.CancelToken.IsCancellationRequested)
                {
                    Thread.Sleep(_delayMs);
                    _task.run();
                }
                _task.cancel();
            }
        }
    }
}
