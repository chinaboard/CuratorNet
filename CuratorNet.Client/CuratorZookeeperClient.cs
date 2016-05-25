using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Client.Drivers;
using Org.Apache.CuratorNet.Client.Ensemble;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.Java.Types;
using Org.Apache.Java.Types.Concurrent.Atomics;

namespace Org.Apache.CuratorNet.Client
{
    /**
     * A wrapper around Zookeeper that takes care of some low-level housekeeping
     */
    public class CuratorZookeeperClient : IDisposable
    {
        private readonly Logger                            log = LogManager.GetCurrentClassLogger();
        private readonly ConnectionState                   state;
        private readonly AtomicReference<IRetryPolicy>     retryPolicy = new AtomicReference<IRetryPolicy>();
        private readonly int connectionTimeoutMs;
        private readonly AtomicBoolean                     started = new AtomicBoolean(false);
        private readonly AtomicReference<ITracerDriver>     tracer 
            = new AtomicReference<ITracerDriver>(new DefaultTracerDriver());

        /**
         *
         * @param connectString list of servers to connect to
         * @param sessionTimeoutMs session timeout
         * @param connectionTimeoutMs connection timeout
         * @param watcher default watcher or null
         * @param retryPolicy the retry policy to use
         */
        public CuratorZookeeperClient(String connectString, 
                                        int sessionTimeoutMs, 
                                        int connectionTimeoutMs, 
                                        Watcher watcher, 
                                        IRetryPolicy retryPolicy) 
            : this(new DefaultZookeeperFactory(),
                    new FixedEnsembleProvider(connectString),
                    sessionTimeoutMs,
                    connectionTimeoutMs,
                    watcher,
                    retryPolicy,
                    false)
        {
            
        }

        /**
         * @param ensembleProvider the ensemble provider
         * @param sessionTimeoutMs session timeout
         * @param connectionTimeoutMs connection timeout
         * @param watcher default watcher or null
         * @param retryPolicy the retry policy to use
         */
        public CuratorZookeeperClient(IEnsembleProvider ensembleProvider, 
                                        int sessionTimeoutMs, 
                                        int connectionTimeoutMs, 
                                        Watcher watcher, 
                                        IRetryPolicy retryPolicy)
            : this(new DefaultZookeeperFactory(),
                    ensembleProvider,
                    sessionTimeoutMs,
                    connectionTimeoutMs,
                    watcher,
                    retryPolicy,
                    false)
        {
        }

        /**
         * @param zookeeperFactory factory for creating {@link ZooKeeper} instances
         * @param ensembleProvider the ensemble provider
         * @param sessionTimeoutMs session timeout
         * @param connectionTimeoutMs connection timeout
         * @param watcher default watcher or null
         * @param retryPolicy the retry policy to use
         * @param canBeReadOnly if true, allow ZooKeeper client to enter
         *                      read only mode in case of a network partition. See
         *                      {@link ZooKeeper#ZooKeeper(String, int, Watcher, long, byte[], boolean)}
         *                      for details
         */
        public CuratorZookeeperClient(IZookeeperFactory zookeeperFactory, 
                                        IEnsembleProvider ensembleProvider,
                                        int sessionTimeoutMs, 
                                        int connectionTimeoutMs, 
                                        Watcher watcher, 
                                        IRetryPolicy retryPolicy, 
                                        bool canBeReadOnly)
        {
            if (ensembleProvider == null)
            {
                throw new ArgumentNullException(nameof(ensembleProvider),
                                                "ensembleProvider cannot be null");
            }
            if (retryPolicy == null)
            {
                throw new ArgumentNullException(nameof(retryPolicy),
                                                "retryPolicy cannot be null");
            }
            if (sessionTimeoutMs < connectionTimeoutMs)
            {
                log.Warn("session timeout [{0}] is less than connection timeout [{1}]", 
                                        sessionTimeoutMs, 
                                        connectionTimeoutMs);
            }
            this.connectionTimeoutMs = connectionTimeoutMs;
            state = new ConnectionState(zookeeperFactory, 
                                            ensembleProvider, 
                                            sessionTimeoutMs, 
                                            connectionTimeoutMs, 
                                            watcher, 
                                            tracer, 
                                            canBeReadOnly);
            setRetryPolicy(retryPolicy);
        }

        /**
         * Return the managed ZK instance.
         *
         * @return client the client
         * @throws Exception if the connection timeout has elapsed or an exception occurs in a background process
         */
        public ZooKeeper getZooKeeper()
        {
            if (!started.get())
            {
                throw new InvalidOperationException("Client is not started");
            }
            return state.getZooKeeper();
        }

        /**
         * Return a new retry loop. All operations should be performed in a retry loop
         *
         * @return new retry loop
         */
        public RetryLoop newRetryLoop()
        {
            return new RetryLoop(retryPolicy.Get(), tracer);
        }

        /**
         * Return a new "session fail" retry loop. See {@link SessionFailRetryLoop} for details
         * on when to use it.
         *
         * @param mode failure mode
         * @return new retry loop
         */
        public SessionFailRetryLoop newSessionFailRetryLoop(SessionFailRetryLoop.Mode mode)
        {
            return new SessionFailRetryLoop(this, mode);
        }

        /**
         * Returns true if the client is current connected
         *
         * @return true/false
         */
        public bool isConnected()
        {
            return state.isConnected();
        }

        /**
         * This method blocks until the connection to ZK succeeds. Use with caution. The block
         * will timeout after the connection timeout (as passed to the constructor) has elapsed
         *
         * @return true if the connection succeeded, false if not
         * @throws InterruptedException interrupted while waiting
         */
        public bool blockUntilConnectedOrTimedOut()
        {
            if (!started.get())
            {
                throw new InvalidOperationException("Client is not started");
            }

            log.Debug("blockUntilConnectedOrTimedOut() start");
            TimeTrace trace = startTracer("blockUntilConnectedOrTimedOut");

            internalBlockUntilConnectedOrTimedOut();

            trace.commit();

            bool localIsConnected = state.isConnected();
            log.Debug("blockUntilConnectedOrTimedOut() end. isConnected: " + localIsConnected);

            return localIsConnected;
        }

        /**
         * Must be called after construction
         *
         * @throws IOException errors
         */
        public void start()
        {
            log.Debug("Starting");

            if ( !started.compareAndSet(false, true) )
            {
                InvalidOperationException ise = new InvalidOperationException("Already started");
                throw ise;
            }

            state.start();
        }

        /**
         * Close the client
         */
        public void Dispose()
        {
            log.Debug("Closing");

            started.set(false);
            try
            {
                state.Dispose();
            }
            catch (IOException e)
            {
                ThreadUtils.checkInterrupted(e);
                log.Error(e, "");
            }
        }

        /**
         * Change the retry policy
         *
         * @param policy new policy
         */
        public void setRetryPolicy(IRetryPolicy policy)
        {
            if (policy == null)
            {
                throw new InvalidOperationException("policy cannot be null");
            }
            retryPolicy.Set(policy);
        }

        /**
         * Return the current retry policy
         *
         * @return policy
         */
        public IRetryPolicy getRetryPolicy()
        {
            return retryPolicy.Get();
        }

        /**
         * Start a new tracer
         * @param name name of the event
         * @return the new tracer ({@link TimeTrace#commit()} must be called)
         */
        public TimeTrace startTracer(String name)
        {
            return new TimeTrace(name, tracer.Get());
        }

        /**
         * Return the current tracing driver
         *
         * @return tracing driver
         */
        public ITracerDriver getTracerDriver()
        {
            return tracer.Get();
        }

        /**
         * Change the tracing driver
         *
         * @param tracer new tracing driver
         */
        public void setTracerDriver(ITracerDriver tracer)
        {
            this.tracer.Set(tracer);
        }

        /**
         * Returns the current known connection string - not guaranteed to be correct
         * value at any point in the future.
         *
         * @return connection string
         */
        public String getCurrentConnectionString()
        {
            return state.getEnsembleProvider().getConnectionString();
        }

        /**
         * Return the configured connection timeout
         *
         * @return timeout
         */
        public int getConnectionTimeoutMs()
        {
            return connectionTimeoutMs;
        }

        /**
         * Every time a new {@link ZooKeeper} instance is allocated, the "instance index"
         * is incremented.
         *
         * @return the current instance index
         */
        public long getInstanceIndex()
        {
            return state.getInstanceIndex();
        }

        internal void addParentWatcher(Watcher watcher)
        {
            state.addParentWatcher(watcher);
        }

        internal void removeParentWatcher(Watcher watcher)
        {
            state.removeParentWatcher(watcher);
        }

        internal void internalBlockUntilConnectedOrTimedOut()
        {
            TimeSpan waitTimeMs = TimeSpan.FromMilliseconds(connectionTimeoutMs);
            while ( !state.isConnected() && (waitTimeMs > TimeSpan.Zero) )
            {
                Barrier latch = new Barrier(1);
                Watcher tempWatcher = new ConnectionSuccessWatcher(latch);
                state.addParentWatcher(tempWatcher);
                DateTime startTimeMs = DateTime.Now;
                try
                {
                    latch.SignalAndWait(TimeSpan.FromSeconds(1));
                }
                finally
                {
                    state.removeParentWatcher(tempWatcher);
                }
                waitTimeMs -= DateTime.Now - startTimeMs;
            }
        }

        private class ConnectionSuccessWatcher : Watcher
        {
            private readonly Barrier _barrier;

            internal ConnectionSuccessWatcher(Barrier barrier)
            {
                _barrier = barrier;
            }

            public override Task process(WatchedEvent @event)
            {
                _barrier.SignalAndWait();
                return Task.FromResult<object>(null);
            }
        }
    }
}
