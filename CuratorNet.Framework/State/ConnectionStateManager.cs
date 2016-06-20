using System;
using System.Collections.Concurrent;
using System.Threading;
using NLog;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.Java.Types;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Atomics;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.CuratorNet.Framework.State
{
    /**
     * Used internally to manage connection state
     */
    public class ConnectionStateManager : IDisposable
    {
        private const int LATENT = 0;
        private const int STARTED = 1;
        private const int CLOSED = 2;
        private readonly int QUEUE_SIZE;

        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly BlockingCollection<ConnectionState> eventQueue = new BlockingCollection<ConnectionState>(QUEUE_SIZE);
        private readonly CuratorFramework client;
        private readonly ListenerContainer<IConnectionStateListener> listeners = new ListenerContainer<IConnectionStateListener>();
        private readonly AtomicBoolean initialConnectMessageSent = new AtomicBoolean(false);
        private readonly IExecutorService service;
        private readonly AtomicInteger state = new AtomicInteger(LATENT);
        private readonly ManualResetEvent stateChangeEvent = new ManualResetEvent(false);

        // guarded by sync
        private ConnectionState currentConnectionState = ConnectionState.LOST;
        private readonly object _setSuspendedLock = new object();
        private readonly object _setStateChangedLock = new object();
        private readonly object _blockLock = new object();
        private readonly object _isConnectedLock = new object();

        /**
         * @param client        the client
         * @param threadFactory thread factory to use or null for a default
         */
        public ConnectionStateManager(CuratorFramework client, int queueSize)
        {
            this.client = client;
            service = ThreadUtils.newSingleThreadExecutor(GetType().FullName);
            QUEUE_SIZE = queueSize <= 0 ? 25 : queueSize;
        }

        /**
         * Start the manager
         */
        public void start()
        {
            if (state.CompareAndSet(LATENT, STARTED) != LATENT)
            {
                throw new InvalidOperationException("Cannot be started more than once");
            }
            service.submit
            (
                new FutureTask<object>(CallableUtils.FromFunc<object>(() =>
                {
                    processEvents();
                    return null;
                }))
            );
        }

        public void Dispose()
        {
            if (state.CompareAndSet(STARTED, CLOSED) == STARTED)
            {
                service.Dispose();
                listeners.clear();
            }
        }

        /**
         * Return the listenable
         *
         * @return listenable
         */
        public ListenerContainer<IConnectionStateListener> getListenable()
        {
            return listeners;
        }

        /**
         * Change to {@link ConnectionState#SUSPENDED} only if not already suspended and not lost
         * 
         * @return true if connection is set to SUSPENDED
         */
        public bool setToSuspended()
        {
            lock (_setSuspendedLock)
            {
                if (state.Get() != STARTED)
                {
                    return false;
                }

                if (currentConnectionState == ConnectionState.LOST
                        || currentConnectionState == ConnectionState.SUSPENDED)
                {
                    return false;
                }

                currentConnectionState = ConnectionState.SUSPENDED;
                postState(ConnectionState.SUSPENDED);

                return true;
            }
        }

        /**
         * Post a state change. If the manager is already in that state the change
         * is ignored. Otherwise the change is queued for listeners.
         *
         * @param newConnectionState new state
         * @return true if the state actually changed, false if it was already at that state
         */
        public bool addStateChange(ConnectionState newConnectionState)
        {
            lock (_setStateChangedLock)
            {
                if (state.Get() != STARTED)
                {
                    return false;
                }

                ConnectionState previousState = currentConnectionState;
                if (previousState == newConnectionState)
                {
                    return false;
                }
                currentConnectionState = newConnectionState;

                ConnectionState localState = newConnectionState;
                bool isNegativeMessage = ((newConnectionState == ConnectionState.LOST) || (newConnectionState == ConnectionState.SUSPENDED) || (newConnectionState == ConnectionState.READ_ONLY));
                if (!isNegativeMessage && initialConnectMessageSent.compareAndSet(false, true))
                {
                    localState = ConnectionState.CONNECTED;
                }

                postState(localState);

                return true;
            }
        }

        public bool blockUntilConnected(int maxWaitTime)
        {
            lock (_blockLock)
            {
                long startTime = DateTimeUtils.GetCurrentMs();
                bool hasMaxWait = maxWaitTime > 0;
                long maxWaitTimeMs = hasMaxWait ? maxWaitTime : 0;
                while ( !isConnected() )
                {
                    if (hasMaxWait)
                    {
                        long waitTime = maxWaitTimeMs - (DateTimeUtils.GetCurrentMs() - startTime);
                        if (waitTime <= 0)
                        {
                            return isConnected();
                        }
                        stateChangeEvent.WaitOne((int) waitTime);
                    }
                    else
                    {
                        stateChangeEvent.WaitOne();
                    }
                }
                return isConnected();
            }
        }

        public bool isConnected()
        {
            lock (_isConnectedLock)
            {
                return ConnectionStateUtils.IsConnected(currentConnectionState);
            }
        }

        private void postState(ConnectionState state)
        {
            log.Info("State change: " + state);
            stateChangeEvent.Set();
            while (!eventQueue.TryAdd(state))
            {
                ConnectionState value;
                eventQueue.TryTake(out value);
                log.Warn("ConnectionStateManager queue full - dropping events to make room");
            }
        }

        private void processEvents()
        {
            while (state.Get() == STARTED)
            {
//                try
//                {
                    ConnectionState newState = eventQueue.Take();

                    if (listeners.size() == 0)
                    {
                        log.Warn("There are no ConnectionStateListeners registered.");
                    }

                    listeners.forEach
                    (
                         listener =>
                         {
                             listener.stateChanged(client, newState);
                             return null;
                         }
                    );
//                }
//                catch ( Exception e )
//                {
//                    // swallow the interrupt as it's only possible from either a background
//                    // operation and, thus, doesn't apply to this loop or the instance
//                    // is being closed in which case the while test will get it
//                }
            }
        }
    }
}