using NLog;
using System;
using System.Collections.Concurrent;
using System.Threading;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Atomics;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.CuratorNet.Client.Utils
{
    /**
     * Decoration on an ExecutorService that tracks created futures and provides
     * a method to close futures created via this class
     */
    public class CloseableExecutorService : IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly ConcurrentDictionary<IFuture<object>, IFuture<object>> futures 
            = new ConcurrentDictionary<IFuture<object>, IFuture<object>>();
        private readonly IExecutorService execService;
        protected readonly AtomicBoolean isOpen = new AtomicBoolean(true);

        protected class InternalScheduledFutureTask : IFuture<object>
        {
            private readonly CloseableExecutorService _executorService;
            private readonly IFuture<object> _scheduledFuture;

            public InternalScheduledFutureTask(CloseableExecutorService executorService, 
                                                IFuture<object> scheduledFuture)
            {
                _executorService = executorService;
                _scheduledFuture = scheduledFuture;
                executorService.futures.TryAdd(scheduledFuture, scheduledFuture);
            }

            public bool cancel()
            {
                IFuture<object> value;
                _executorService.futures.TryRemove(_scheduledFuture,out value);
                return _scheduledFuture.cancel();
            }

            public bool isCancelled()
            {
                return _scheduledFuture.isCancelled();
            }

            public bool isDone()
            {
                return _scheduledFuture.isDone();
            }

            public object get()
            {
                return null;
            }

            public object get(int timeout)
            {
                return null;
            }
        }

        protected class InternalFutureTask<T> : FutureTask<T> where T : class
        {
            private readonly CloseableExecutorService _executorService;
            private readonly IRunnableFuture<T> _task;

            internal InternalFutureTask(CloseableExecutorService executorService, IRunnableFuture<T> task) 
                : base(task)
            {
                _executorService = executorService;
                _task = task;
                _executorService.futures.TryAdd(task, task);
            }

            protected override void done()
            {
                IFuture<object> value;
                _executorService.futures.TryRemove(_task, out value);
            }
        }
        
        /**
         * @param executorService the service to decorate
         * @param shutdownOnClose if true, shutdown the executor service when this is closed
         */
        public CloseableExecutorService(IExecutorService execService)
        {
            if (execService == null)
            {
                throw new ArgumentNullException(nameof(execService));
            }
            this.execService = execService;
        }

        /**
         * Returns <tt>true</tt> if this executor has been shut down.
         *
         * @return <tt>true</tt> if this executor has been shut down
         */
        public bool isShutdown()
        {
            return !isOpen.get();
        }

        public int size()
        {
            return futures.Count;
        }

        /**
         * Closes any tasks currently in progress
         */
        public void Dispose()
        {
            isOpen.set(false);
            foreach (var kv in futures)
            {
                IFuture<object> future;
                futures.TryRemove(kv.Key, out future);
                future = kv.Key;
                if (!future.isDone() && !future.isCancelled() && !future.cancel())
                {
                    log.Warn("Could not cancel " + future);
                }
            }
        }

        public IFuture<T> submit<T>(ICallable<T> task) where T : class
        {
            if (!isOpen.get())
            {
                throw new InvalidOperationException("CloseableExecutorService is closed");
            }
            InternalFutureTask<T> futureTask = new InternalFutureTask<T>(this, new FutureTask<T>(task));
            return execService.submit(futureTask);
        }

        public IFuture<object> submit(IRunnable task)
        {
            if (!isOpen.get())
            {
                throw new InvalidOperationException("CloseableExecutorService is closed");
            }
            InternalFutureTask<object> futureTask 
                = new InternalFutureTask<object>(this, new FutureTask<object>(task));
            return execService.submit(futureTask);
        }

        public IFuture<T> submit<T>(ICallable<T> task, CancellationTokenSource token) where T : class
        {
            if (!isOpen.get())
            {
                throw new InvalidOperationException("CloseableExecutorService is closed");
            }
            InternalFutureTask<T> futureTask = new InternalFutureTask<T>(this, new FutureTask<T>(task, token));
            return execService.submit(futureTask);
        }

        public IFuture<object> submit(IRunnable task, CancellationTokenSource token)
        {
            if (!isOpen.get())
            {
                throw new InvalidOperationException("CloseableExecutorService is closed");
            }
            InternalFutureTask<object> futureTask
                = new InternalFutureTask<object>(this, new FutureTask<object>(task, token));
            return execService.submit(futureTask);
        }
    }    
}
