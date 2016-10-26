using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.CuratorNet.Framework.API.Transaction;
using Org.Apache.CuratorNet.Framework.Listen;
using Org.Apache.CuratorNet.Framework.State;
using Org.Apache.Java.Types;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Atomics;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    public class CuratorFrameworkImpl : CuratorFramework
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly CuratorZookeeperClient client;
        private readonly ListenerContainer<ICuratorListener> listeners;
        private readonly ListenerContainer<IUnhandledErrorListener> unhandledErrorListeners;
        private readonly int maxCloseWaitMs;
        private readonly BlockingCollection<OperationAndData<object>> backgroundOperations;
        private readonly NamespaceImpl @namespace;
        private readonly ConnectionStateManager connectionStateManager;
        private readonly List<AuthInfo> authInfos;
        private readonly byte[] defaultData;
        private readonly FailedDeleteManager failedDeleteManager;
        private readonly ICompressionProvider compressionProvider;
        private readonly IACLProvider aclProvider;
        private readonly NamespaceFacadeCache namespaceFacadeCache;
        private readonly NamespaceWatcherMap namespaceWatcherMap;
        private readonly bool _useContainerParentsIfAvailable;

        private volatile IExecutorService executorService;
        private readonly AtomicBoolean logAsErrorConnectionErrors = new AtomicBoolean(false);

        private static readonly bool LOG_ALL_CONNECTION_ISSUES_AS_ERROR_LEVEL = true;

        internal interface DebugBackgroundListener
        {
            void listen(OperationAndData<object> data);
        }

        internal volatile DebugBackgroundListener debugListener = null;

        public volatile IUnhandledErrorListener debugUnhandledErrorListener = null;

        private readonly AtomicReference<CuratorFrameworkState> state;

        private class WatchedWatcher : Watcher
        {
            private readonly CuratorFrameworkImpl _curatorFrameworkImpl;

            public WatchedWatcher(CuratorFrameworkImpl curatorFrameworkImpl)
            {
                _curatorFrameworkImpl = curatorFrameworkImpl;
            }

            public override Task process(WatchedEvent watchedEvent)
            {
                string unfixForNs = _curatorFrameworkImpl.unfixForNamespace(watchedEvent.getPath());
                ICuratorEvent @event = new CuratorEventImpl(_curatorFrameworkImpl, 
                                                            CuratorEventType.WATCHED, 
                                                            (int) watchedEvent.getState(), 
                                                            unfixForNs, 
                                                            null, 
                                                            null, 
                                                            null, 
                                                            null, 
                                                            null, 
                                                            watchedEvent, 
                                                            null);
                processEvent(@event);
                return Task.FromResult<object>(null);
            }
        }

        public CuratorFrameworkImpl(CuratorFrameworkFactory.Builder builder)
        {
            IZookeeperFactory localZookeeperFactory = makeZookeeperFactory(builder.GetZookeeperFactory());
            this.client = new CuratorZookeeperClient(localZookeeperFactory, 
                                                        builder.GetEnsembleProvider(), 
                                                        builder.GetSessionTimeoutMs(), 
                                                        builder.GetConnectionTimeoutMs(), 
                                                        new WatchedWatcher(this), 
                                                        builder.GetRetryPolicy(), 
                                                        builder.CanBeReadOnly());

            listeners = new ListenerContainer<ICuratorListener>();
            unhandledErrorListeners = new ListenerContainer<IUnhandledErrorListener>();
            backgroundOperations = new DelayQueue<OperationAndData<object>>();
            @namespace = new NamespaceImpl(this, builder.GetNamespace());
            maxCloseWaitMs = builder.GetMaxCloseWaitMs();
            connectionStateManager = new ConnectionStateManager(this, builder.GetEventQueueSize());
            compressionProvider = builder.GetCompressionProvider();
            aclProvider = builder.GetAclProvider();
            state = new AtomicReference<CuratorFrameworkState>(CuratorFrameworkState.LATENT);
            useContainerParentsIfAvailable = builder.UseContainerParentsIfAvailable();

            byte[] builderDefaultData = builder.GetDefaultData();
            byte[] destDefaults = new byte[builderDefaultData.Length];
            Array.Copy(builderDefaultData, destDefaults, builderDefaultData.Length);
            defaultData = (builderDefaultData != null) 
                            ?  destDefaults
                            : new byte[0];
            authInfos = buildAuths(builder);

            failedDeleteManager = new FailedDeleteManager(this);
            namespaceWatcherMap = new NamespaceWatcherMap(this);
            namespaceFacadeCache = new NamespaceFacadeCache(this);
        }

        private List<AuthInfo> BuildAuths(CuratorFrameworkFactory.Builder builder)
        {
            List<AuthInfo> builder1 = new List<AuthInfo>();
            if (builder.GetAuthInfos() != null)
            {
                builder1.AddRange(builder.GetAuthInfos());
            }
            return builder1;
        }

        private class InternalZookeeperFactory : IZookeeperFactory
        {
            private readonly CuratorFrameworkImpl _factory;
            private readonly IZookeeperFactory _actualZookeeperFactory;

            public InternalZookeeperFactory(CuratorFrameworkImpl factory, IZookeeperFactory zookeeperFactory)
            {
                _factory = factory;
                _actualZookeeperFactory = zookeeperFactory;
            }

            public ZooKeeper newZooKeeper(String connectString, int sessionTimeout, Watcher watcher, bool canBeReadOnly)
            {
                ZooKeeper zooKeeper = _actualZookeeperFactory.newZooKeeper(connectString, sessionTimeout, watcher, canBeReadOnly);
                foreach (AuthInfo auth in _factory.authInfos)
                {
                    zooKeeper.addAuthInfo(auth.getScheme(), auth.getAuth());
                }
                return zooKeeper;
            }
        }

        private IZookeeperFactory makeZookeeperFactory(IZookeeperFactory actualZookeeperFactory)
        {
            return new InternalZookeeperFactory(this, actualZookeeperFactory);
        }

        protected CuratorFrameworkImpl(CuratorFrameworkImpl parent)
        {
            client = parent.client;
            listeners = parent.listeners;
            unhandledErrorListeners = parent.unhandledErrorListeners;
            maxCloseWaitMs = parent.maxCloseWaitMs;
            backgroundOperations = parent.backgroundOperations;
            connectionStateManager = parent.connectionStateManager;
            defaultData = parent.defaultData;
            failedDeleteManager = parent.failedDeleteManager;
            compressionProvider = parent.compressionProvider;
            aclProvider = parent.aclProvider;
            namespaceFacadeCache = parent.namespaceFacadeCache;
            @namespace = new NamespaceImpl(this, null);
            state = parent.state;
            authInfos = parent.authInfos;
            _useContainerParentsIfAvailable = parent._useContainerParentsIfAvailable;
        }

        public void createContainers(String path)
        {
            checkExists().creatingParentContainersIfNeeded().forPath(ZKPaths.makePath(path, "foo"));
        }

        public void clearWatcherReferences(Watcher watcher)
        {
            NamespaceWatcher namespaceWatcher = namespaceWatcherMap.remove(watcher);
            if (namespaceWatcher != null)
            {
                namespaceWatcher.Dispose();
            }
        }

        public CuratorFrameworkState getState()
        {
            return state.Get();
        }

        [Obsolete]
        public bool isStarted()
        {
            return state.Get() == CuratorFrameworkState.STARTED;
        }

        public bool blockUntilConnected(int maxWaitTimeMs)
        {
            return connectionStateManager.blockUntilConnected(maxWaitTimeMs);
        }

        public void blockUntilConnected()
        {
            blockUntilConnected(0);
        }

        private class ConnectedStateListener : IConnectionStateListener
        {
            public void stateChanged(CuratorFramework client, ConnectionState newState)
            {
                if (ConnectionState.CONNECTED == newState || ConnectionState.RECONNECTED == newState)
                {
                    logAsErrorConnectionErrors.set(true);
                }
            }
        }

        public void start()
        {
            log.Info("Starting");
            if (!state.CompareAndSet(CuratorFrameworkState.LATENT, CuratorFrameworkState.STARTED))
            {
                throw new InvalidOperationException("Cannot be started more than once");
            }
            try
            {
                connectionStateManager.start(); // ordering dependency - must be called before client.start()

                IConnectionStateListener listener = new ConnectedStateListener();

                this.getConnectionStateListenable().addListener(listener);

                client.start();

                executorService = ThreadUtils.newSingleThreadScheduledExecutor(GetType().FullName);
                executorService.submit(new FutureTask<object>(RunnableUtils.FromFunc(() => backgroundOperationsLoop())));
            }
            catch ( Exception e )
            {
                ThreadUtils.checkInterrupted(e);
                handleBackgroundOperationException(null, e);
            }
        }

        public void close()
        {
            log.Debug("Closing");
            if (state.CompareAndSet(CuratorFrameworkState.STARTED, CuratorFrameworkState.STOPPED))
            {
                listeners.forEach(listener =>
                {
                    ICuratorEvent @event = new CuratorEventImpl(this, CuratorEventType.CLOSING, 0, null, null, null, null, null, null, null, null);
                    try
                    {
                        listener.eventReceived(this, @event);
                    }
                    catch (Exception e)
                    {
                        ThreadUtils.checkInterrupted(e);
                        log.Error("Exception while sending Closing event", e);
                    }
                    return null;
                });

                if ( executorService != null )
                {
                    executorService.Dispose();
                }

                listeners.clear();
                unhandledErrorListeners.clear();
                connectionStateManager.Dispose();
                client.Dispose();
                namespaceWatcherMap.Dispose();
            }
        }

        [Obsolete]
        public CuratorFramework nonNamespaceView()
        {
            return usingNamespace(null);
        }

        public String getNamespace()
        {
            String str = @namespace.getNamespace();
            return (str != null) ? str : "";
        }

        private void CheckStarted()
        {
            if (getState() != CuratorFrameworkState.STARTED)
            {
                throw new InvalidOperationException("instance must be started before calling this method");
            }
        }

        public CuratorFramework usingNamespace(String newNamespace)
        {
            CheckStarted();
            return namespaceFacadeCache.get(newNamespace);
        }

        public ICreateBuilder create()
        {
            CheckStarted();
            return new CreateBuilderImpl(this);
        }

        public DeleteBuilder delete()
        {
            CheckStarted();
            return new DeleteBuilderImpl(this);
        }

        public IExistsBuilder checkExists()
        {
            CheckStarted();
            return new ExistsBuilderImpl(this);
        }

        public IGetDataBuilder getData()
        {
            CheckStarted();
            return new GetDataBuilderImpl(this);
        }

        public ISetDataBuilder setData()
        {
            CheckStarted();
            return new SetDataBuilderImpl(this);
        }

        public IGetChildrenBuilder getChildren()
        {
            CheckStarted();
            return new GetChildrenBuilderImpl(this);
        }

        public IGetACLBuilder getACL()
        {
            CheckStarted();
            return new GetACLBuilderImpl(this);
        }

        public ISetACLBuilder setACL()
        {
            CheckStarted();

            return new SetACLBuilderImpl(this);
        }

        public ICuratorTransaction inTransaction()
        {
            CheckStarted();
            return new CuratorTransactionImpl(this);
        }

        public Listenable<IConnectionStateListener> getConnectionStateListenable()
        {
            return connectionStateManager.getListenable();
        }

        public Listenable<ICuratorListener> getCuratorListenable()
        {
            return listeners;
        }

        public Listenable<IUnhandledErrorListener> getUnhandledErrorListenable()
        {
            return unhandledErrorListeners;
        }

        public void sync(String path, Object context)
        {
            CheckStarted();
            path = fixForNamespace(path);
            internalSync(this, path, context);
        }

        public ISyncBuilder sync()
        {
            return new SyncBuilderImpl(this);
        }

        protected void internalSync(CuratorFrameworkImpl impl, String path, Object context)
        {
            IBackgroundOperation<String> operation = new BackgroundSyncImpl(impl, context);
            performBackgroundOperation(new OperationAndData<String>(operation, path, null, null, context));
        }

        public CuratorZookeeperClient getZookeeperClient()
        {
            return client;
        }

        public EnsurePath newNamespaceAwareEnsurePath(String path)
        {
            return @namespace.newNamespaceAwareEnsurePath(path);
        }

        internal IACLProvider getAclProvider()
        {
            return aclProvider;
        }

        internal FailedDeleteManager getFailedDeleteManager()
        {
            return failedDeleteManager;
        }

        internal RetryLoop newRetryLoop()
        {
            return client.newRetryLoop();
        }

        internal ZooKeeper getZooKeeper()
        {
            return client.getZooKeeper();
        }

        internal ICompressionProvider getCompressionProvider()
        {
            return compressionProvider;
        }

        internal bool useContainerParentsIfAvailable()
        {
            return _useContainerParentsIfAvailable;
        }

        internal void processBackgroundOperation<DATA_TYPE>(OperationAndData<DATA_TYPE> operationAndData, 
                                                            ICuratorEvent @event)
        {
            bool isInitialExecution = (@event == null);
            if (isInitialExecution)
            {
                performBackgroundOperation(operationAndData);
                return;
            }

            bool doQueueOperation = false;
            do
            {
                if (RetryLoop.shouldRetry(@event.getResultCode()))
                {
                    doQueueOperation = checkBackgroundRetry(operationAndData, @event);
                    break;
                }

                if (operationAndData.getCallback() != null)
                {
                    sendToBackgroundCallback(operationAndData, @event);
                    break;
                }

                processEvent(@event);
            }
            while (false);

            if (doQueueOperation)
            {
                queueOperation(operationAndData);
            }
        }

        internal void queueOperation<DATA_TYPE>(OperationAndData<DATA_TYPE> operationAndData)
        {
            if (getState() == CuratorFrameworkState.STARTED)
            {
                backgroundOperations.offer(operationAndData);
            }
        }

        internal void logError(String reason, Exception e)
        {
            if ((reason == null) || (reason.Length == 0))
            {
                reason = "n/a";
            }

            if (e is KeeperException.ConnectionLossException)
            {
                if (LOG_ALL_CONNECTION_ISSUES_AS_ERROR_LEVEL || logAsErrorConnectionErrors.compareAndSet(true, false))
                {
                    log.Error(reason, e);
                }
                else
                {
                    log.Debug(reason, e);
                }
            }
            if (!(e is KeeperException) )
            {
                log.Error(reason, e);
            }

            String localReason = reason;
            unhandledErrorListeners.forEach(new Function<IUnhandledErrorListener, Void>()
            {
                public Void apply(UnhandledErrorListener listener)
                {
                    listener.unhandledError(localReason, e);
                    return null;
                }
            });

            if ( debugUnhandledErrorListener != null )
            {
                debugUnhandledErrorListener.unhandledError(reason, e);
            }
        }

        internal String unfixForNamespace(String path)
        {
            return @namespace.unfixForNamespace(path);
        }

        internal String fixForNamespace(String path)
        {
            return @namespace.fixForNamespace(path, false);
        }

        internal String fixForNamespace(String path, bool isSequential)
        {
            return @namespace.fixForNamespace(path, isSequential);
        }

        internal byte[] getDefaultData()
        {
            return defaultData;
        }

        internal NamespaceFacadeCache getNamespaceFacadeCache()
        {
            return namespaceFacadeCache;
        }

        internal NamespaceWatcherMap getNamespaceWatcherMap()
        {
            return namespaceWatcherMap;
        }

        internal void validateConnection(Watcher.Event.KeeperState state)
        {
            if (state == Watcher.Event.KeeperState.Disconnected)
            {
                suspendConnection();
            }
            else if (state == Watcher.Event.KeeperState.Expired)
            {
                connectionStateManager.addStateChange(ConnectionState.LOST);
            }
            else if (state == Watcher.Event.KeeperState.SyncConnected)
            {
                connectionStateManager.addStateChange(ConnectionState.RECONNECTED);
            }
            else if (state == Watcher.Event.KeeperState.ConnectedReadOnly)
            {
                connectionStateManager.addStateChange(ConnectionState.READ_ONLY);
            }
        }

        internal Watcher.Event.KeeperState codeToState(KeeperException code)
        {
            if (code is KeeperException.AuthFailedException || code is KeeperException.NoAuthException)
            {
                return Watcher.Event.KeeperState.AuthFailed;
            }
            if (code is KeeperException.ConnectionLossException || code is KeeperException.OperationTimeoutException)
            {
                return Watcher.Event.KeeperState.Disconnected;
            }
            if (code is KeeperException.SessionExpiredException)
            {
                return Watcher.Event.KeeperState.Expired;
            }
            if (code is KeeperException.SessionMovedException)
            {
                return Watcher.Event.KeeperState.SyncConnected;
            }
            throw new InvalidOperationException();
            return Watcher.Event.KeeperState.Disconnected;
        }

        private void suspendConnection()
        {
            if (!connectionStateManager.setToSuspended())
            {
                return;
            }

            doSyncForSuspendedConnection(client.getInstanceIndex());
        }

        private void doSyncForSuspendedConnection(long instanceIndex)
        {
            // we appear to have disconnected, force a new ZK event and see if we can connect to another server
            BackgroundOperation< String > operation = new BackgroundSyncImpl(this, null);
            OperationAndData.ErrorCallback<String> errorCallback = new OperationAndData.ErrorCallback<String>()
            {
                @Override
                public void retriesExhausted(OperationAndData<String> operationAndData)
                {
                    // if instanceIndex != newInstanceIndex, the ZooKeeper instance was reset/reallocated
                    // so the pending background sync is no longer valid.
                    // if instanceIndex is -1, this is the second try to sync - punt and mark the connection lost
                    if ((instanceIndex < 0) || (instanceIndex == client.getInstanceIndex()))
                    {
                        connectionStateManager.addStateChange(ConnectionState.LOST);
                    }
                    else
                    {
                        log.Debug("suspendConnection() failure ignored as the ZooKeeper instance was reset. Retrying.");
                        // send -1 to signal that if it happens again, punt and mark the connection lost
                        doSyncForSuspendedConnection(-1);
                    }
                }
            };
            performBackgroundOperation(new OperationAndData<String>(operation, "/", null, errorCallback, null));
        }

        private bool checkBackgroundRetry<DATA_TYPE>(OperationAndData<DATA_TYPE> operationAndData, ICuratorEvent @event)
        {
            bool doRetry = false;
            if (client.getRetryPolicy().allowRetry(operationAndData.getThenIncrementRetryCount(), operationAndData.getElapsedTimeMs(), operationAndData))
            {
                doRetry = true;
            }
            else
            {
                if (operationAndData.getErrorCallback() != null)
                {
                    operationAndData.getErrorCallback().retriesExhausted(operationAndData);
                }

                if (operationAndData.getCallback() != null)
                {
                    sendToBackgroundCallback(operationAndData, @event);
                }

                KeeperException.Code code = KeeperException.Code.get(@event.getResultCode());
                Exception e = null;
                try
                {
                    e = (code != null) ? KeeperException.create(code) : null;
                }
                catch (Exception t)
                {
                    ThreadUtils.checkInterrupted(t);
                }
                if (e == null)
                {
                    e = new Exception("Unknown result codegetResultCode()");
                }

                validateConnection(codeToState(code));
                logError("Background operation retry gave up", e);
            }
            return doRetry;
        }

        private KeeperException ExceptionFromCode(int code)
            {
                if (!Enum.IsDefined(typeof (KeeperException.Code), code))
                {
                    throw new ArgumentOutOfRangeException("code", "Invalid exception code");
                }
                KeeperException.Code codeEnum = Enum.ToObject(typeof(KeeperException.Code), code);
                switch (EnumUtil<KeeperException.Code>.DefinedCast((object)code))
                {
                    case KeeperException.Code.NOTREADONLY:
                        return (KeeperException)new KeeperException.NotReadOnlyException();
                    case KeeperException.Code.SESSIONMOVED:
                        return (KeeperException)new KeeperException.SessionMovedException();
                    case KeeperException.Code.AUTHFAILED:
                        return (KeeperException)new KeeperException.AuthFailedException();
                    case KeeperException.Code.INVALIDACL:
                        return (KeeperException)new KeeperException.InvalidACLException(path);
                    case KeeperException.Code.INVALIDCALLBACK:
                        return (KeeperException)new KeeperException.InvalidCallbackException();
                    case KeeperException.Code.SESSIONEXPIRED:
                        return (KeeperException)new KeeperException.SessionExpiredException();
                    case KeeperException.Code.NOTEMPTY:
                        return (KeeperException)new KeeperException.NotEmptyException(path);
                    case KeeperException.Code.NODEEXISTS:
                        return (KeeperException)new KeeperException.NodeExistsException(path);
                    case KeeperException.Code.NOCHILDRENFOREPHEMERALS:
                        return (KeeperException)new KeeperException.NoChildrenForEphemeralsException(path);
                    case KeeperException.Code.BADVERSION:
                        return (KeeperException)new KeeperException.BadVersionException(path);
                    case KeeperException.Code.NOAUTH:
                        return (KeeperException)new KeeperException.NoAuthException();
                    case KeeperException.Code.NONODE:
                        return (KeeperException)new KeeperException.NoNodeException(path);
                    case KeeperException.Code.BADARGUMENTS:
                        return (KeeperException)new KeeperException.BadArgumentsException(path);
                    case KeeperException.Code.OPERATIONTIMEOUT:
                        return (KeeperException)new KeeperException.OperationTimeoutException();
                    case KeeperException.Code.UNIMPLEMENTED:
                        return (KeeperException)new KeeperException.UnimplementedException();
                    case KeeperException.Code.MARSHALLINGERROR:
                        return (KeeperException)new KeeperException.MarshallingErrorException();
                    case KeeperException.Code.CONNECTIONLOSS:
                        return (KeeperException)new KeeperException.ConnectionLossException();
                    case KeeperException.Code.DATAINCONSISTENCY:
                        return (KeeperException)new KeeperException.DataInconsistencyException();
                    case KeeperException.Code.RUNTIMEINCONSISTENCY:
                        return (KeeperException)new KeeperException.RuntimeInconsistencyException();
                    default:
                        throw new ArgumentOutOfRangeException("code", "Invalid exception code");
                }
            }

        private void sendToBackgroundCallback<DATA_TYPE>(OperationAndData<DATA_TYPE> operationAndData, ICuratorEvent @event)
        {
            try
            {
                operationAndData.getCallback().processResult(this, @event);
            }
            catch (Exception e)
            {
                ThreadUtils.checkInterrupted(e);
                handleBackgroundOperationException(operationAndData, e);
            }
        }

        private void handleBackgroundOperationException<DATA_TYPE>(OperationAndData<DATA_TYPE> operationAndData, Exception e)
        {
            do
            {
                if ((operationAndData != null) && RetryLoop.isRetryException(e))
                {
                    log.Debug("Retry-able exception received", e);
                    if (client.getRetryPolicy().allowRetry(operationAndData.getThenIncrementRetryCount(), 
                                                            operationAndData.getElapsedTimeMs(), 
                                                            operationAndData))
                    {
                        log.Debug("Retrying operation");
                        backgroundOperations.offer(operationAndData);
                        break;
                    }
                    else
                    {
                        log.Debug("Retry policy did not allow retry");
                        if (operationAndData.getErrorCallback() != null)
                        {
                            operationAndData.getErrorCallback().retriesExhausted(operationAndData);
                        }
                    }
                }

                logError("Background exception was not retry-able or retry gave up", e);
            }
            while (false);
        }

        private void backgroundOperationsLoop()
        {
            try
            {
                while (state.get() == CuratorFrameworkState.STARTED)
                {
                    OperationAndData <?> operationAndData;
                    try
                    {
                        operationAndData = backgroundOperations.take();
                        if (debugListener != null)
                        {
                            debugListener.listen(operationAndData);
                        }
                        performBackgroundOperation(operationAndData);
                    }
                    catch (InterruptedException e)
                    {
                        // swallow the interrupt as it's only possible from either a background
                        // operation and, thus, doesn't apply to this loop or the instance
                        // is being closed in which case the while test will get it
                    }
                }
            }
            finally
            {
                log.info("backgroundOperationsLoop exiting");
            }
        }

        private void performBackgroundOperation(OperationAndData<?> operationAndData)
        {
            try
            {
                if (client.isConnected())
                {
                    operationAndData.callPerformBackgroundOperation();
                }
                else
                {
                    client.getZooKeeper();  // important - allow connection resets, timeouts, etc. to occur
                    if (operationAndData.getElapsedTimeMs() >= client.getConnectionTimeoutMs())
                    {
                        throw new CuratorConnectionLossException();
                    }
                    operationAndData.sleepFor(1, TimeUnit.SECONDS);
                    queueOperation(operationAndData);
                }
            }
            catch (Exception e)
            {
                ThreadUtils.checkInterrupted(e);
                /**
                 * Fix edge case reported as CURATOR-52. ConnectionState.checkTimeouts() throws KeeperException.ConnectionLossException
                 * when the initial (or previously failed) connection cannot be re-established. This needs to be run through the retry policy
                 * and callbacks need to get invoked, etc.
                 */
                if (e is CuratorConnectionLossException )
                {
                    WatchedEvent watchedEvent = new WatchedEvent(Watcher.Event.EventType.None, Watcher.Event.KeeperState.Disconnected, null);
                    ICuratorEvent @event = new CuratorEventImpl(this, CuratorEventType.WATCHED, 
                                                                    KeeperException.Code.CONNECTIONLOSS.intValue(), 
                                                                    null, 
                                                                    null, 
                                                                    operationAndData.getContext(), 
                                                                    null, 
                                                                    null, 
                                                                    null, 
                                                                    watchedEvent, 
                                                                    null);
                    if (checkBackgroundRetry(operationAndData, @event))
                    {
                        queueOperation(operationAndData);
                    }
                    else
                    {
                        logError("Background retry gave up", e);
                    }
                }
                else
                {
                    handleBackgroundOperationException(operationAndData, e);
                }
            }
        }

        private void processEvent(ICuratorEvent curatorEvent)
        {
            if (curatorEvent.getType() == CuratorEventType.WATCHED)
            {
                validateConnection(curatorEvent.getWatchedEvent().getState());
            }

            listeners.forEach(new Function<ICuratorListener, Void>()
            {
                    @Override
                    public Void apply(CuratorListener listener)
                    {
                        try
                        {
                            TimeTrace trace = client.startTracer("EventListener");
                            listener.eventReceived(CuratorFrameworkImpl.this, curatorEvent);
                            trace.commit();
                        }
                        catch (Exception e)
                        {
                            ThreadUtils.checkInterrupted(e);
                            logError("Event listener threw exception", e);
                        }
                        return null;
                    }
            });
        }
    }
}