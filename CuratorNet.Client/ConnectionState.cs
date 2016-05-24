using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using NLog;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Client.Drivers;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.Java.Types.Concurrent.Atomics;

namespace Org.Apache.CuratorNet.Client
{
    class ConnectionState : Watcher, IDisposable
    {
        private static readonly int MAX_BACKGROUND_EXCEPTIONS = 10;
        private static readonly bool LOG_EVENTS = true;
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly HandleHolder zooKeeper;
        private readonly AtomicBoolean isConnected = new AtomicBoolean(false);
        private readonly int sessionTimeoutMs;
        private readonly int connectionTimeoutMs;
        private readonly AtomicReference<ITracerDriver> tracer;
        private readonly ConcurrentQueue<Exception> backgroundExceptions = new ConcurrentQueue<Exception>();
        private readonly ConcurrentQueue<Watcher> parentWatchers = new ConcurrentQueue<Watcher>();
        private readonly AtomicLong instanceIndex = new AtomicLong();
        private volatile long connectionStartMs = 0;

        internal ConnectionState(IZookeeperFactory zookeeperFactory, 
                        int sessionTimeoutMs, 
                        int connectionTimeoutMs, 
                        Watcher parentWatcher, 
                        AtomicReference<ITracerDriver> tracer, 
                        bool canBeReadOnly)
        {
            this.sessionTimeoutMs = sessionTimeoutMs;
            this.connectionTimeoutMs = connectionTimeoutMs;
            this.tracer = tracer;
            if (parentWatcher != null)
            {
                parentWatchers.Enqueue(parentWatcher);
            }

            zooKeeper = new HandleHolder(zookeeperFactory, this, sessionTimeoutMs, canBeReadOnly);
        }

        internal ZooKeeper getZooKeeper()
        {
            if ( SessionFailRetryLoop.sessionForThreadHasFailed() )
            {
                throw new SessionFailRetryLoop.SessionFailedException();
            }

            Exception exception = backgroundExceptions.poll();
            if ( exception != null )
            {
                tracer.get().addCount("background-exceptions", 1);
                throw exception;
            }

            bool localIsConnected = isConnected.get();
            if (!localIsConnected)
            {
                checkTimeouts();
            }

            return zooKeeper.getZooKeeper();
        }

        bool isConnected()
        {
            return isConnected.get();
        }

        void start()
        {
            log.debug("Starting");
            ensembleProvider.start();
            reset();
        }

        public void close()
        {
            log.Debug("Closing");

            CloseableUtils.closeQuietly(ensembleProvider);
            try
            {
                zooKeeper.closeAndClear();
            }
            catch ( Exception e )
            {
                ThreadUtils.checkInterrupted(e);
                throw new IOException(e);
            }
            finally
            {
                isConnected.set(false);
            }
        }

        void addParentWatcher(Watcher watcher)
        {
            parentWatchers.offer(watcher);
        }

        void removeParentWatcher(Watcher watcher)
        {
            parentWatchers.remove(watcher);
        }

        long getInstanceIndex()
        {
            return instanceIndex.get();
        }

        @Override
        public void process(WatchedEvent event)
        {
            if (LOG_EVENTS)
            {
                log.debug("ConnectState watcher: " + event);
            }

            if ( event.getType() == Watcher.Event.EventType.None )
            {
                boolean wasConnected = isConnected.get();
                boolean newIsConnected = checkState(event.getState(), wasConnected);
                if (newIsConnected != wasConnected)
                {
                    isConnected.set(newIsConnected);
                    connectionStartMs = System.currentTimeMillis();
                }
            }

            for (Watcher parentWatcher : parentWatchers)
            {
                TimeTrace timeTrace = new TimeTrace("connection-state-parent-process", tracer.get());
                parentWatcher.process(event);
                timeTrace.commit();
            }
        }

        EnsembleProvider getEnsembleProvider()
        {
            return ensembleProvider;
        }

        private synchronized void checkTimeouts() throws Exception
        {
            int minTimeout = Math.min(sessionTimeoutMs, connectionTimeoutMs);
            long elapsed = System.currentTimeMillis() - connectionStartMs;
            if ( elapsed >= minTimeout )
            {
                if (zooKeeper.hasNewConnectionString())
                {
                    handleNewConnectionString();
                }
                else
                {
                    int maxTimeout = Math.max(sessionTimeoutMs, connectionTimeoutMs);
                    if (elapsed > maxTimeout)
                    {
                        if (!Boolean.getBoolean(DebugUtils.PROPERTY_DONT_LOG_CONNECTION_ISSUES))
                        {
                            log.warn(String.format("Connection attempt unsuccessful after %d (greater than max timeout of %d). Resetting connection and trying again with a new connection.", elapsed, maxTimeout));
                        }
                        reset();
                    }
                    else
                    {
                        KeeperException.ConnectionLossException connectionLossException = new CuratorConnectionLossException();
                        if (!Boolean.getBoolean(DebugUtils.PROPERTY_DONT_LOG_CONNECTION_ISSUES))
                        {
                            log.error(String.format("Connection timed out for connection string (%s) and timeout (%d) / elapsed (%d)", zooKeeper.getConnectionString(), connectionTimeoutMs, elapsed), connectionLossException);
                        }
                        tracer.get().addCount("connections-timed-out", 1);
                        throw connectionLossException;
                    }
                }
            }
        }

        private synchronized void reset() throws Exception
        {
            log.debug("reset");

            instanceIndex.incrementAndGet();

            isConnected.set(false);
            connectionStartMs = System.currentTimeMillis();
            zooKeeper.closeAndReset();
            zooKeeper.getZooKeeper();   // initiate connection
            }

        private boolean checkState(Event.KeeperState state, boolean wasConnected)
        {
            boolean isConnected = wasConnected;
            boolean checkNewConnectionString = true;
            switch (state)
            {
                default:
                case Disconnected:
                    {
                        isConnected = false;
                        break;
                    }

                case SyncConnected:
                case ConnectedReadOnly:
                    {
                        isConnected = true;
                        break;
                    }

                case AuthFailed:
                    {
                        isConnected = false;
                        log.error("Authentication failed");
                        break;
                    }

                case Expired:
                    {
                        isConnected = false;
                        checkNewConnectionString = false;
                        handleExpiredSession();
                        break;
                    }

                case SaslAuthenticated:
                    {
                        // NOP
                        break;
                    }
            }

            if (checkNewConnectionString && zooKeeper.hasNewConnectionString())
            {
                handleNewConnectionString();
            }

            return isConnected;
        }

        private void handleNewConnectionString()
        {
            log.info("Connection string changed");
            tracer.get().addCount("connection-string-changed", 1);

            try
            {
                reset();
            }
            catch (Exception e)
            {
                ThreadUtils.checkInterrupted(e);
                queueBackgroundException(e);
            }
        }

        private void handleExpiredSession()
        {
            log.warn("Session expired event received");
            tracer.get().addCount("session-expired", 1);

            try
            {
                reset();
            }
            catch (Exception e)
            {
                ThreadUtils.checkInterrupted(e);
                queueBackgroundException(e);
            }
        }

        @SuppressWarnings({ "ThrowableResultOfMethodCallIgnored"})
        private void queueBackgroundException(Exception e)
        {
            while (backgroundExceptions.size() >= MAX_BACKGROUND_EXCEPTIONS)
            {
                backgroundExceptions.poll();
            }
            backgroundExceptions.offer(e);
        }
    }
}
