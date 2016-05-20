using NLog;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Atomics;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.CuratorNet.Client.Utils
{
    /**
     * Decoration on an ExecutorService that tracks created futures and provides
     * a method to close futures created via this class
     */
    public class CloseableExecutorService : IExecutorService, IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly ConcurrentDictionary<IFuture<object>, IFuture<object>> futures 
            = new ConcurrentDictionary<IFuture<object>, IFuture<object>>();
        private readonly IExecutorService executorService;
        private readonly bool shutdownOnClose;
        protected readonly AtomicBoolean isOpen = new AtomicBoolean(true);

        protected class InternalScheduledFutureTask : IFuture<object>
        {
            private readonly CloseableExecutorService _executorService;
            private readonly IScheduledFuture<object> scheduledFuture;

            public InternalScheduledFutureTask(CloseableExecutorService executorService, 
                                                IScheduledFuture<object> scheduledFuture)
            {
                _executorService = executorService;
                this.scheduledFuture = scheduledFuture;
                executorService.futures.TryAdd(scheduledFuture, scheduledFuture);
            }

            public bool cancel()
            {
                IFuture<object> value;
                _executorService.futures.TryRemove(scheduledFuture,out value);
                return scheduledFuture.cancel();
            }

            public bool isCancelled()
            {
                return scheduledFuture.isCancelled();
            }

            public bool isDone()
            {
                return scheduledFuture.isDone();
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

            internal InternalFutureTask(CloseableExecutorService executorService, Task<T> task) 
                : base(task)
            {
                _executorService = executorService;
                var futureTask = new FutureTask<T>(task);
                _executorService.futures.TryAdd(futureTask, futureTask);
                task.ContinueWith(mainTask =>
                {
                    IFuture<object> value;
                    _executorService.futures.TryRemove(futureTask, out value);
                });
            }
        }

        /**
         * @param executorService the service to decorate
         */
        public CloseableExecutorService(IExecutorService executorService) 
            : this(executorService, false)
        {
        }

        /**
         * @param executorService the service to decorate
         * @param shutdownOnClose if true, shutdown the executor service when this is closed
         */
        public CloseableExecutorService(IExecutorService executorService, bool shutdownOnClose)
        {
            if (executorService == null)
            {
                throw new ArgumentNullException(nameof(executorService));
            }
            this.executorService = executorService;
            this.shutdownOnClose = shutdownOnClose;
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

        int size()
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

//        public IFuture<T> submit<T>(Func<T> task) where T : class
//        {
//            if (!isOpen.get())
//            {
//                throw new InvalidOperationException("CloseableExecutorService is closed");
//            }
//            return executorService.submit(task);
//        }
//
//        public IFuture<object> submit(Action task)
//        {
//            if (!isOpen.get())
//            {
//                throw new InvalidOperationException("CloseableExecutorService is closed");
//            }
//            return executorService.submit(task);
//        }
    }    
}
