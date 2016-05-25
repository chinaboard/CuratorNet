using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Atomics;

namespace Org.Apache.CuratorNet.Client
{
    /**
     * <p>
     *     See {@link RetryLoop} for the main details on retry loops. <b>All Curator/ZooKeeper operations
     *     should be done in a retry loop.</b>
     * </p>
     *
     * <p>
     *     The standard retry loop treats session failure as a type of connection failure. i.e. the fact
     *     that it is a session failure isn't considered. This can be problematic if you are performing
     *     a series of operations that rely on ephemeral nodes. If the session fails after the ephemeral
     *     node has been created, future Curator/ZooKeeper operations may succeed even though the
     *     ephemeral node has been removed by ZooKeeper.
     * </p>
     *
     * <p>
     *     Here's an example:
     * </p>
     *     <ul>
     *         <li>You create an ephemeral/sequential node as a kind of lock/marker</li>
     *         <li>You perform some other operations</li>
     *         <li>The session fails for some reason</li>
     *         <li>You attempt to create a node assuming that the lock/marker still exists
     *         <ul>
     *             <li>Curator will notice the session failure and try to reconnect</li>
     *             <li>In most cases, the reconnect will succeed and, thus, the node creation will succeed
     *             even though the ephemeral node will have been deleted by ZooKeeper.</li>
     *         </ul>
     *         </li>
     *     </ul>
     *
     * <p>
     *     The SessionFailRetryLoop prevents this type of scenario. When a session failure is detected,
     *     the thread is marked as failed which will cause all future Curator operations to fail. The
     *     SessionFailRetryLoop will then either retry the entire
     *     set of operations or fail (depending on {@link SessionFailRetryLoop.Mode})
     * </p>
     *
     * Canonical usage:<br>
     * <pre>
     * SessionFailRetryLoop    retryLoop = client.newSessionFailRetryLoop(mode);
     * retryLoop.start();
     * try
     * {
     *     while ( retryLoop.shouldContinue() )
     *     {
     *         try
     *         {
     *             // do work
     *         }
     *         catch ( Exception e )
     *         {
     *             retryLoop.takeException(e);
     *         }
     *     }
     * }
     * finally
     * {
     *     retryLoop.close();
     * }
     * </pre>
     */
    public class SessionFailRetryLoop : IDisposable
    {
        private readonly CuratorZookeeperClient    client;
        private readonly Mode                      mode;
        private readonly Thread                    ourThread = Thread.CurrentThread;
        private readonly AtomicBoolean             sessionHasFailed = new AtomicBoolean(false);
        private readonly AtomicBoolean             isDone = new AtomicBoolean(false);
        private readonly RetryLoop                 retryLoop;

        private class SessionFailWatcher : Watcher
        {
            private readonly SessionFailRetryLoop _retryLoop;
            public SessionFailWatcher(SessionFailRetryLoop retryLoop)
            {
                _retryLoop = retryLoop;
            }

            public override Task process(WatchedEvent @event)
            {
                if ( @event.getState() == Watcher.Event.KeeperState.Expired )
                {
                    _retryLoop.sessionHasFailed.set(true);
                    SessionFailRetryLoop.failedSessionThreads
                                        .AddOrUpdate(_retryLoop.ourThread, 
                                                    _retryLoop.ourThread,
                                                    (thread, thread1) => _retryLoop.ourThread);
                }
                return Task.FromResult<object>(null);
            }
        }

        private readonly Watcher watcher;


        private static readonly ConcurrentDictionary<Thread, Thread> failedSessionThreads 
            = new ConcurrentDictionary<Thread, Thread>();

        public class SessionFailedException : ApplicationException {}

        public enum Mode
        {
            /**
             * If the session fails, retry the entire set of operations when {@link SessionFailRetryLoop#shouldContinue()}
             * is called
             */
            RETRY,

            /**
             * If the session fails, throw {@link KeeperException.SessionExpiredException} when
             * {@link SessionFailRetryLoop#shouldContinue()} is called
             */
            FAIL
        }

        /**
         * Convenience utility: creates a "session fail" retry loop calling the given proc
         *
         * @param client Zookeeper
         * @param mode how to handle session failures
         * @param proc procedure to call with retry
         * @param <T> return type
         * @return procedure result
         * @throws Exception any non-retriable errors
         */
        public static T callWithRetry<T>(CuratorZookeeperClient client, Mode mode, ICallable<T> proc)
        {
            T result = default(T);
            SessionFailRetryLoop retryLoop = client.newSessionFailRetryLoop(mode);
            retryLoop.start();
                try
                {
                while (retryLoop.shouldContinue())
                {
                    try
                    {
                        result = proc.call();
                    }
                    catch (Exception e)
                    {
                        ThreadUtils.checkInterrupted(e);
                        retryLoop.takeException(e);
                    }
                }
            }
                finally
                {
                retryLoop.Dispose();
            }
                return result;
        }

        internal SessionFailRetryLoop(CuratorZookeeperClient client, Mode mode)
        {
            this.client = client;
            this.mode = mode;
            retryLoop = client.newRetryLoop();
            watcher = new SessionFailWatcher(this);
        }

        internal static bool sessionForThreadHasFailed()
        {
            return (failedSessionThreads.Count > 0) 
                    && failedSessionThreads.ContainsKey(Thread.CurrentThread);
        }

        /**
         * SessionFailRetryLoop must be started
         */
        public void start()
        {
            if (Thread.CurrentThread != ourThread)
            {
                throw new InvalidOperationException("Not in the correct thread");
            }
            client.addParentWatcher(watcher);
        }

        /**
         * If true is returned, make an attempt at the set of operations
         *
         * @return true/false
         */
        public bool shouldContinue()
        {
            bool localIsDone = isDone.getAndSet(true);
            return !localIsDone;
        }

        /**
         * Must be called in a finally handler when done with the loop
         */
        public void Dispose()
        {
            if (Thread.CurrentThread != ourThread)
            {
                throw new InvalidOperationException("Not in the correct thread");
            }
            Thread value;
            failedSessionThreads.TryRemove(ourThread,out value);

            client.removeParentWatcher(watcher);
        }

        /**
         * Pass any caught exceptions here
         *
         * @param exception the exception
         * @throws Exception if not retry-able or the retry policy returned negative
         */
        public void takeException(Exception exception)
        {
            if (Thread.CurrentThread != ourThread)
            {
                throw new InvalidOperationException("Not in the correct thread");
            }

            bool passUp = true;
            if ( sessionHasFailed.get() )
            {
                switch (mode)
                {
                    case Mode.RETRY:
                    {
                        sessionHasFailed.set(false);
                        Thread value;
                        failedSessionThreads.TryRemove(ourThread, out value);
                        if (exception is SessionFailedException )
                        {
                            isDone.set(false);
                            passUp = false;
                        }
                        break;
                    }
                    case Mode.FAIL:
                    {
                        break;
                    }
                }
            }

            if (passUp)
            {
                retryLoop.takeException(exception);
            }
        }
    }
}
