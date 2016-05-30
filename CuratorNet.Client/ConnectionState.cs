using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Client.Drivers;
using Org.Apache.CuratorNet.Client.Ensemble;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.Java.Types.Concurrent.Atomics;

namespace Org.Apache.CuratorNet.Client
{
    internal class ConnectionState : Watcher, IDisposable
    {
        private static readonly int MAX_BACKGROUND_EXCEPTIONS = 10;
        private static readonly bool LOG_EVENTS = true;
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly HandleHolder zooKeeper;
        private readonly AtomicBoolean _isConnected = new AtomicBoolean(false);
        private readonly IEnsembleProvider ensembleProvider;
        private readonly int sessionTimeoutMs;
        private readonly int connectionTimeoutMs;
        private readonly AtomicReference<ITracerDriver> tracer;
        private readonly ConcurrentQueue<Exception> backgroundExceptions 
            = new ConcurrentQueue<Exception>();
        private volatile ConcurrentQueue<Watcher> parentWatchers = new ConcurrentQueue<Watcher>();
        private readonly AtomicLong instanceIndex = new AtomicLong();
        private long connectionStartMs;

        private readonly object _timeoutLock = new object();
        private readonly object _resetLock = new object();

        internal ConnectionState(IZookeeperFactory zookeeperFactory, 
                        IEnsembleProvider ensembleProvider, 
                        int sessionTimeoutMs, 
                        int connectionTimeoutMs, 
                        Watcher parentWatcher, 
                        AtomicReference<ITracerDriver> tracer, 
                        bool canBeReadOnly)
        {
            this.ensembleProvider = ensembleProvider;
            this.sessionTimeoutMs = sessionTimeoutMs;
            this.connectionTimeoutMs = connectionTimeoutMs;
            this.tracer = tracer;
            if (parentWatcher != null)
            {
                parentWatchers.Enqueue(parentWatcher);
            }

            zooKeeper = new HandleHolder(zookeeperFactory, this, ensembleProvider, sessionTimeoutMs, canBeReadOnly);
        }

        internal ZooKeeper getZooKeeper()
        {
            if ( SessionFailRetryLoop.sessionForThreadHasFailed() )
            {
                throw new SessionFailRetryLoop.SessionFailedException();
            }
            Exception exception;
            backgroundExceptions.TryDequeue(out exception);
            if ( exception != null )
            {
                tracer.Get().addCount("background-exceptions", 1);
                throw exception;
            }

            bool localIsConnected = _isConnected.get();
            if (!localIsConnected)
            {
                checkTimeouts();
            }

            return zooKeeper.getZooKeeper();
        }

        internal bool isConnected()
        {
            return _isConnected.get();
        }

        internal void start()
        {
            log.Debug("Connection starting");
            ensembleProvider.start();
            reset();
        }

        public void Dispose()
        {
            log.Debug("Connection Closing");

            CloseableUtils.closeQuietly(ensembleProvider);
            try
            {
                zooKeeper.closeAndClear();
            }
            catch ( Exception e )
            {
                ThreadUtils.checkInterrupted(e);
                throw new IOException("",e);
            }
            finally
            {
                _isConnected.set(false);
            }
        }

        internal void addParentWatcher(Watcher watcher)
        {
            parentWatchers.Enqueue(watcher);
        }

        internal void removeParentWatcher(Watcher watcher)
        {
            // TODO: this is workaround to remove element from queue in C#
            // Java version of queue has remove method
            ConcurrentQueue<Watcher> newQueue = new ConcurrentQueue<Watcher>();
            foreach (Watcher parentWatcher in parentWatchers)
            {
                if (parentWatcher != watcher)
                {
                    newQueue.Enqueue(parentWatcher);
                }
            }
            parentWatchers = newQueue;
        }

        internal long getInstanceIndex()
        {
            return instanceIndex.Get();
        }

        public override Task process(WatchedEvent @event)
        {
            if (LOG_EVENTS)
            {
                log.Debug("ConnectState watcher: " + @event);
            }

            if ( @event.get_Type() == Watcher.Event.EventType.None )
            {
                bool wasConnected = _isConnected.get();
                bool newIsConnected = checkState(@event.getState(), wasConnected);
                if ( newIsConnected != wasConnected )
                {
                    _isConnected.set(newIsConnected);
                    Volatile.Write(ref connectionStartMs, CurrentMillis);
                }
            }

            foreach ( Watcher parentWatcher in parentWatchers )
            {
                TimeTrace timeTrace = new TimeTrace("connection-state-parent-process", tracer.Get());
                parentWatcher.process(@event);
                timeTrace.commit();
            }
            return Task.FromResult<object>(null);
        }

        private static long CurrentMillis
        {
            get { return DateTime.Now.Ticks / 1000; }
        }

        internal IEnsembleProvider getEnsembleProvider()
        {
            return ensembleProvider;
        }

        private void checkTimeouts()
        {
            lock (_timeoutLock)
            {
                int minTimeout = Math.Min(sessionTimeoutMs, connectionTimeoutMs);
                long elapsed = CurrentMillis - Volatile.Read(ref connectionStartMs);
                if ( elapsed >= minTimeout )
                {
                    if (zooKeeper.hasNewConnectionString())
                    {
                        handleNewConnectionString();
                    }
                    else
                    {
                        int maxTimeout = Math.Max(sessionTimeoutMs, connectionTimeoutMs);
                        if (elapsed > maxTimeout)
                        {
                            log.Warn("Connection attempt unsuccessful after {0} " 
                                                    + "(greater than max timeout of {1}). Resetting " 
                                                    + "connection and trying again with a new connection.", 
                                                    elapsed, maxTimeout);
                            reset();
                        }
                        else
                        {
                            var connectionLossException = new CuratorConnectionLossException();
                            log.Error(String.Format("Connection timed out for connection string "
                                                    + "({0}) and timeout ({1}) / elapsed ({2})", 
                                                    zooKeeper.getConnectionString(), 
                                                    connectionTimeoutMs, 
                                                    elapsed), 
                                                    connectionLossException);
                            tracer.Get().addCount("connections-timed-out", 1);
                            throw connectionLossException;
                        }
                    }
                }
            }
        }

        private void reset()
        {
            lock (_resetLock)
            {
                log.Debug("Connection reset");

                instanceIndex.IncrementAndGet();

                _isConnected.set(false);
                Volatile.Write(ref connectionStartMs, CurrentMillis);
                zooKeeper.closeAndReset();
                zooKeeper.getZooKeeper();   // initiate connection
            }
        }

        private bool checkState(Event.KeeperState state, bool wasConnected)
        {
            bool isConnected = wasConnected;
            bool checkNewConnectionString = true;
            switch (state)
            {
                case Event.KeeperState.Disconnected:
                {
                    isConnected = false;
                    break;
                }

                case Event.KeeperState.SyncConnected:
                case Event.KeeperState.ConnectedReadOnly:
                {
                    isConnected = true;
                    break;
                }

                case Event.KeeperState.AuthFailed:
                {
                    isConnected = false;
                    log.Error("Authentication failed");
                    break;
                }

                case Event.KeeperState.Expired:
                {
                    isConnected = false;
                    checkNewConnectionString = false;
                    handleExpiredSession();
                    break;
                }

                default :
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
            log.Info("Connection string changed");
            tracer.Get().addCount("connection-string-changed", 1);

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
            log.Warn("Session expired event received");
            tracer.Get().addCount("session-expired", 1);

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

        private void queueBackgroundException(Exception e)
        {
            while (backgroundExceptions.Count >= MAX_BACKGROUND_EXCEPTIONS)
            {
                Exception value;
                backgroundExceptions.TryDequeue(out value);
            }
            backgroundExceptions.Enqueue(e);
        }
    }
}
