using System;
using System.Collections.Generic;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.CuratorNet.Framework.API.Transaction;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Atomics;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class CreateBuilderImpl : ICreateBuilder, 
                                        IBackgroundOperation<PathAndBytes>, 
                                        ErrorListenerPathAndBytesable<String>
    {
        private readonly CuratorFrameworkImpl client;
        private CreateMode createMode;
        private Backgrounding backgrounding;
        private bool createParentsIfNeeded;
        private bool createParentsAsContainers;
        private bool doProtected;
        private bool compress;
        private String protectedId;
        private ACLing acling;

        bool failNextCreateForTesting = false;

        static readonly String PROTECTED_PREFIX = "_c_";

        CreateBuilderImpl(CuratorFrameworkImpl client)
        {
            this.client = client;
            createMode = CreateMode.PERSISTENT;
            backgrounding = new Backgrounding();
            acling = new ACLing(client.getAclProvider());
            createParentsIfNeeded = false;
            createParentsAsContainers = false;
            compress = false;
            doProtected = false;
            protectedId = null;
        }

        private class TransactionCreateBuilder : ITransactionCreateBuilder
        {
            private readonly CreateBuilderImpl _createBuilderImpl;
            private readonly CuratorTransactionImpl _curatorTransaction;
            private readonly CuratorMultiTransactionRecord _transaction;

            public TransactionCreateBuilder(CreateBuilderImpl createBuilderImpl,
                                            CuratorTransactionImpl curatorTransaction,
                                            CuratorMultiTransactionRecord transaction)
            {
                _createBuilderImpl = createBuilderImpl;
                _curatorTransaction = curatorTransaction;
                _transaction = transaction;
            }

            public PathAndBytesable<ICuratorTransactionBridge> withACL(List<ACL> aclList)
            {
                _createBuilderImpl.withACL(aclList);
                return this;
            }

            public IACLPathAndBytesable<ICuratorTransactionBridge> withMode(CreateMode mode)
            {
                _createBuilderImpl.withMode(mode);
                return this;
            }

            public IACLCreateModePathAndBytesable<ICuratorTransactionBridge> compressed()
            {
                _createBuilderImpl.compressed();
                return this;
            }

            public ICuratorTransactionBridge forPath(String path)
            {
                return forPath(path, client.getDefaultData());
            }

            public ICuratorTransactionBridge forPath(String path, byte[] data)
            {
                if (compress)
                {
                    data = client.getCompressionProvider().compress(path, data);
                }

                String fixedPath = client.fixForNamespace(path);
                _transaction.add(Op.create(fixedPath, data, acling.getAclList(path), createMode), OperationType.CREATE, path);
                return _curatorTransaction;
            }
        }

        ITransactionCreateBuilder asTransactionCreateBuilder(CuratorTransactionImpl curatorTransaction, 
                                                                CuratorMultiTransactionRecord transaction)
        {
            return new TransactionCreateBuilder(this, curatorTransaction, transaction);
        }

        private class CreateBackgroundModeACLable : ICreateBackgroundModeACLable
        {
            private readonly CreateBuilderImpl _createBuilderImpl;

            public CreateBackgroundModeACLable(CreateBuilderImpl createBuilderImpl)
            {
                _createBuilderImpl = createBuilderImpl;
            }

            public IACLCreateModePathAndBytesable<String> creatingParentsIfNeeded()
            {
                _createBuilderImpl.createParentsIfNeeded = true;
                return _createBuilderImpl.asACLCreateModePathAndBytesable();
            }

            public IACLCreateModePathAndBytesable<String> creatingParentContainersIfNeeded()
            {
                _createBuilderImpl.setCreateParentsAsContainers();
                return creatingParentsIfNeeded();
            }

            public IACLPathAndBytesable<String> withProtectedEphemeralSequential()
            {
                return _createBuilderImpl.withProtectedEphemeralSequential();
            }

            public IBackgroundPathAndBytesable<String> withACL(List<ACL> aclList)
            {
                return _createBuilderImpl.withACL(aclList);
            }

            public ErrorListenerPathAndBytesable<String> inBackground(IBackgroundCallback callback, Object context)
            {
                return _createBuilderImpl.inBackground(callback, context);
            }

            public ErrorListenerPathAndBytesable<String> inBackground(IBackgroundCallback callback, Object context, IExecutor executor)
            {
                return _createBuilderImpl.inBackground(callback, context, executor);
            }

            public ErrorListenerPathAndBytesable<String> inBackground()
            {
                return _createBuilderImpl.inBackground();
            }

            public ErrorListenerPathAndBytesable<String> inBackground(Object context)
            {
                return _createBuilderImpl.inBackground(context);
            }

            public ErrorListenerPathAndBytesable<String> inBackground(IBackgroundCallback callback)
            {
                return _createBuilderImpl.inBackground(callback);
            }

            public ErrorListenerPathAndBytesable<String> inBackground(IBackgroundCallback callback, IExecutor executor)
            {
                return _createBuilderImpl.inBackground(callback, executor);
            }

            public IACLBackgroundPathAndBytesable<String> withMode(CreateMode mode)
            {
                return _createBuilderImpl.withMode(mode);
            }

            public String forPath(String path, byte[] data)
            {
                return _createBuilderImpl.forPath(path, data);
            }

            public String forPath(System.String path)
            {
                return _createBuilderImpl.forPath(path);
            }
        }

        public ICreateBackgroundModeACLable compressed()
        {
            compress = true;
            return new CreateBackgroundModeACLable(this);
        }

        private class ACLBackgroundPathAndBytesable : IACLBackgroundPathAndBytesable<String>
        {
            private readonly CreateBuilderImpl _createBuilderImpl;

            public ACLBackgroundPathAndBytesable(CreateBuilderImpl createBuilderImpl)
            {
                _createBuilderImpl = createBuilderImpl;
            }

            public IBackgroundPathAndBytesable<String> withACL(List<ACL> aclList)
            {
                return _createBuilderImpl.withACL(aclList);
            }

            public ErrorListenerPathAndBytesable<String> inBackground()
            {
                return _createBuilderImpl.inBackground();
            }

            public ErrorListenerPathAndBytesable<String> inBackground(IBackgroundCallback callback, Object context)
            {
                return _createBuilderImpl.inBackground(callback, context);
            }

            public ErrorListenerPathAndBytesable<String> inBackground(IBackgroundCallback callback, Object context, IExecutor executor)
            {
                return _createBuilderImpl.inBackground(callback, context, executor);
            }

            public ErrorListenerPathAndBytesable<String> inBackground(Object context)
            {
                return _createBuilderImpl.inBackground(context);
            }

            public ErrorListenerPathAndBytesable<String> inBackground(IBackgroundCallback callback)
            {
                return _createBuilderImpl.inBackground(callback);
            }

            public ErrorListenerPathAndBytesable<String> inBackground(IBackgroundCallback callback, IExecutor executor)
            {
                return _createBuilderImpl.inBackground(callback, executor);
            }

            public String forPath(String path, byte[] data)
            {
                return _createBuilderImpl.forPath(path, data);
            }

            public String forPath(System.String path)
            {
                return _createBuilderImpl.forPath(path);
            }
        }

        public IACLBackgroundPathAndBytesable<String> withACL(List<ACL> aclList)
        {
            acling = new ACLing(client.getAclProvider(), aclList);
            return new ACLBackgroundPathAndBytesable(this);
        }

        public ProtectACLCreateModePathAndBytesable<String> creatingParentContainersIfNeeded()
        {
            setCreateParentsAsContainers();
            return creatingParentsIfNeeded();
        }

        private void setCreateParentsAsContainers()
        {
            if (client.useContainerParentsIfAvailable())
            {
                createParentsAsContainers = true;
            }
        }

        private class ProtectACLCreateModePathAndBytesableImpl : ProtectACLCreateModePathAndBytesable<String>
        {
            private readonly CreateBuilderImpl _createBuilderImpl;

            public ProtectACLCreateModePathAndBytesableImpl(CreateBuilderImpl createBuilderImpl)
            {
                _createBuilderImpl = createBuilderImpl;
            }

            public IACLCreateModeBackgroundPathAndBytesable<String> withProtection()
            {
                return _createBuilderImpl.withProtection();
            }

            public IBackgroundPathAndBytesable<String> withACL(List<ACL> aclList)
            {
                return _createBuilderImpl.withACL(aclList);
            }

            public ErrorListenerPathAndBytesable<String> inBackground()
            {
                return _createBuilderImpl.inBackground();
            }

            public ErrorListenerPathAndBytesable<String> inBackground(Object context)
            {
                return _createBuilderImpl.inBackground(context);
            }

            public ErrorListenerPathAndBytesable<String> inBackground(IBackgroundCallback callback)
            {
                return _createBuilderImpl.inBackground(callback);
            }

            public ErrorListenerPathAndBytesable<String> inBackground(IBackgroundCallback callback, Object context)
            {
                return _createBuilderImpl.inBackground(callback, context);
            }

            public ErrorListenerPathAndBytesable<String> inBackground(IBackgroundCallback callback, IExecutor executor)
            {
                return _createBuilderImpl.inBackground(callback, executor);
            }

            public ErrorListenerPathAndBytesable<String> inBackground(IBackgroundCallback callback, Object context, IExecutor executor)
            {
                return _createBuilderImpl.inBackground(callback, context, executor);
            }

            public IACLBackgroundPathAndBytesable<String> withMode(CreateMode mode)
            {
                return _createBuilderImpl.withMode(mode);
            }

            public String forPath(String path, byte[] data)
            {
                return _createBuilderImpl.forPath(path, data);
            }

            public String forPath(System.String path)
            {
                return _createBuilderImpl.forPath(path);
            }
        }

        public ProtectACLCreateModePathAndBytesable<String> creatingParentsIfNeeded()
        {
            createParentsIfNeeded = true;
            return new ProtectACLCreateModePathAndBytesableImpl(this);
        }

        public IACLCreateModeBackgroundPathAndBytesable<String> withProtection()
        {
            setProtected();
            return this;
        }

        private class ACLPathAndBytesable : IACLPathAndBytesable<String>
        {
            private readonly CreateBuilderImpl _createBuilderImpl;

            public ACLPathAndBytesable(CreateBuilderImpl createBuilderImpl)
            {
                _createBuilderImpl = createBuilderImpl;
            }

            public PathAndBytesable<String> withACL(List<ACL> aclList)
            {
                return _createBuilderImpl.withACL(aclList);
            }

            public String forPath(String path, byte[] data)
            {
                return _createBuilderImpl.forPath(path, data);
            }

            public String forPath(System.String path)
            {
                return _createBuilderImpl.forPath(path);
            }
        }

        public IACLPathAndBytesable<String> withProtectedEphemeralSequential()
        {
            setProtected();
            createMode = CreateMode.EPHEMERAL_SEQUENTIAL;

            return new ACLPathAndBytesable(this);
        }

        public IACLBackgroundPathAndBytesable<String> withMode(CreateMode mode)
        {
            createMode = mode;
            return this;
        }

        public ErrorListenerPathAndBytesable<String> inBackground(IBackgroundCallback callback, Object context)
        {
            backgrounding = new Backgrounding(callback, context);
            return this;
        }

        public ErrorListenerPathAndBytesable<String> inBackground(IBackgroundCallback callback, Object context, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, context, executor);
            return this;
        }

        public ErrorListenerPathAndBytesable<String> inBackground(IBackgroundCallback callback)
        {
            backgrounding = new Backgrounding(callback);
            return this;
        }

        public ErrorListenerPathAndBytesable<String> inBackground(IBackgroundCallback callback, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, executor);
            return this;
        }

        public ErrorListenerPathAndBytesable<String> inBackground()
        {
            backgrounding = new Backgrounding(true);
            return this;
        }

        public ErrorListenerPathAndBytesable<String> inBackground(Object context)
        {
            backgrounding = new Backgrounding(context);
            return this;
        }

        public PathAndBytesable<String> withUnhandledErrorListener(IUnhandledErrorListener listener)
        {
            backgrounding = new Backgrounding(backgrounding, listener);
            return this;
        }

        public String forPath(String path)
        {
            return forPath(path, client.getDefaultData());
        }

        public String forPath(String givenPath, byte[] data)
        {
            if ( compress )
            {
                data = client.getCompressionProvider().compress(givenPath, data);
            }

            String adjustedPath = adjustPath(client.fixForNamespace(givenPath, createMode.isSequential()));

            String returnPath = null;
            if ( backgrounding.inBackground() )
            {
                pathInBackground(adjustedPath, data, givenPath);
            }
            else
            {
                String path = protectedPathInForeground(adjustedPath, data);
                returnPath = client.unfixForNamespace(path);
            }
            return returnPath;
        }

        private String protectedPathInForeground(String adjustedPath, byte[] data)
        {
            try
            {
                return pathInForeground(adjustedPath, data);
            }
            catch ( Exception e)
            {
                ThreadUtils.checkInterrupted(e);
                if ((e is KeeperException.ConnectionLossException ||
                    !(e is KeeperException )) && protectedId != null )
                {
                    /*
                     * CURATOR-45 + CURATOR-79: we don't know if the create operation was successful or not,
                     * register the znode to be sure it is deleted later.
                     */
                    new FindAndDeleteProtectedNodeInBackground(client, ZKPaths.getPathAndNode(adjustedPath).getPath(), protectedId).execute();
                    /*
                    * The current UUID is scheduled to be deleted, it is not safe to use it again.
                    * If this builder is used again later create a new UUID
                    */
                    protectedId = UUID.randomUUID().toString();
                }
                throw e;
            }
        }

        public void performBackgroundOperation(OperationAndData<PathAndBytes> operationAndData)
        {
            try
            {
                TimeTrace trace = client.getZookeeperClient().startTracer("CreateBuilderImpl-Background");
                client.getZooKeeper().create
                (
                    operationAndData.getData().getPath(),
                    operationAndData.getData().getData(),
                    acling.getAclList(operationAndData.getData().getPath()),
                    createMode,
                    new AsyncCallback.StringCallback()
                    {
                            @Override
                            public void processResult(int rc, System.String path, Object ctx, System.String name)
                            {
                                trace.commit();

                                if ((rc == KeeperException.Code.NONODE.intValue()) && createParentsIfNeeded)
                                {
                                    backgroundCreateParentsThenNode(client, operationAndData, operationAndData.getData().getPath(), backgrounding, createParentsAsContainers);
                                }
                                else
                                {
                                    sendBackgroundResponse(rc, path, ctx, name, operationAndData);
                                }
                            }
                    },
                    backgrounding.getContext()
                );
            }
            catch ( Exception e )
            {
                backgrounding.checkError(e);
            }
        }

        private static String getProtectedPrefix(String protectedId)
        {
            return PROTECTED_PREFIX + protectedId + "-";
        }

        static void backgroundCreateParentsThenNode<T>(CuratorFrameworkImpl client, 
                                                        OperationAndData<T> mainOperationAndData, 
                                                        String path, 
                                                        IBackgrounding backgrounding, 
                                                        bool createParentsAsContainers)
        {
            IBackgroundOperation<T> operation = new IBackgroundOperation<T>()
            {
                @Override
                public void performBackgroundOperation(OperationAndData<T> dummy)
                {
                    try
                    {
                        ZKPaths.mkdirs(client.getZooKeeper(), path, false, client.getAclProvider(), createParentsAsContainers);
                    }
                    catch ( KeeperException e )
                    {
                        // ignore
                    }
                    client.queueOperation(mainOperationAndData);
                }
            };
            OperationAndData<T> parentOperation = new OperationAndData<T>(operation, mainOperationAndData.getData(), null, null, backgrounding.getContext());
            client.queueOperation(parentOperation);
        }

        private void sendBackgroundResponse(int rc, String path, Object ctx, String name, OperationAndData<PathAndBytes> operationAndData)
        {
            path = client.unfixForNamespace(path);
            name = client.unfixForNamespace(name);

            CuratorEvent event = new CuratorEventImpl(client, CuratorEventType.CREATE, rc, path, name, ctx, null, null, null, null, null);
            client.processBackgroundOperation(operationAndData, event);
        }

        private void setProtected()
        {
            doProtected = true;
            protectedId = UUID.randomUUID().toString();
        }

        class ACLPathAndBytesable : IACLPathAndBytesable<String>
        {
            private readonly CreateBuilderImpl _createBuilderImpl;

            public ACLPathAndBytesable(CreateBuilderImpl createBuilderImpl)
            {
                _createBuilderImpl = createBuilderImpl;
            }

            public PathAndBytesable<String> withACL(List<ACL> aclList)
            {
                return _createBuilderImpl.withACL(aclList);
            }

            public String forPath(String path, byte[] data)
            {
                return _createBuilderImpl.forPath(path, data);
            }

            public String forPath(String path)
            {
                return _createBuilderImpl.forPath(path);
            }
        }

        class ACLCreateModePathAndBytesable : IACLCreateModePathAndBytesable<String>
        {
            private readonly CreateBuilderImpl _createBuilderImpl;

            public ACLCreateModePathAndBytesable(CreateBuilderImpl createBuilderImpl)
            {
                _createBuilderImpl = createBuilderImpl;
            }

            public PathAndBytesable<String> withACL(List<ACL> aclList)
            {
                return _createBuilderImpl.withACL(aclList);
            }

            public ACLPathAndBytesable<String> withMode(CreateMode mode)
            {
                createMode = mode;
                return new ACLPathAndBytesable(this);
            }

            public String forPath(String path, byte[] data)
            {
                return _createBuilderImpl.forPath(path, data);
            }

            public String forPath(System.String path)
            {
                return _createBuilderImpl.forPath(path);
            }
        }

        private IACLCreateModePathAndBytesable<String> asACLCreateModePathAndBytesable()
        {
            return new ACLCreateModePathAndBytesable(this);
        }

        volatile bool debugForceFindProtectedNode = false;

        private void pathInBackground(String path, byte[] data, String givenPath)
        {
            AtomicBoolean firstTime = new AtomicBoolean(true);
            OperationAndData<PathAndBytes> operationAndData = new OperationAndData<PathAndBytes>(this, new PathAndBytes(path, data), backgrounding.getCallback(),
            new OperationAndData.ErrorCallback<PathAndBytes>()
            {
                public void retriesExhausted(OperationAndData<PathAndBytes> operationAndData)
                {
                    if (doProtected)
                    {
                        // all retries have failed, findProtectedNodeInForeground(..) included, schedule a clean up
                        new FindAndDeleteProtectedNodeInBackground(client, ZKPaths.getPathAndNode(path).getPath(), protectedId).execute();
                        // assign a new id if this builder is used again later
                        protectedId = UUID.randomUUID().toString();
                    }
                }
            },
            backgrounding.getContext())
            {
                void callPerformBackgroundOperation()
                {
                    bool callSuper = true;
                    bool localFirstTime = firstTime.getAndSet(false) && !debugForceFindProtectedNode;
                    if ( !localFirstTime && doProtected )
                    {
                        debugForceFindProtectedNode = false;
                        String createdPath = null;
                        try
                        {
                            createdPath = findProtectedNodeInForeground(path);
                        }
                        catch (KeeperException.ConnectionLossException e)
                        {
                            sendBackgroundResponse(KeeperException.Code.CONNECTIONLOSS.intValue(), path, backgrounding.getContext(), null, this);
                            callSuper = false;
                        }
                        if (createdPath != null)
                        {
                            try
                            {
                                sendBackgroundResponse(KeeperException.Code.OK.intValue(), createdPath, backgrounding.getContext(), createdPath, this);
                            }
                            catch (Exception e)
                            {
                                ThreadUtils.checkInterrupted(e);
                                client.logError("Processing protected create for path: " + givenPath, e);
                            }
                            callSuper = false;
                        }
                    }

                    if ( failNextCreateForTesting )
                    {
                        pathInForeground(path, data);   // simulate success on server without notification to client
                        failNextCreateForTesting = false;
                        throw new KeeperException.ConnectionLossException();
                    }

                    if ( callSuper )
                    {
                        super.callPerformBackgroundOperation();
                    }
                }
            };
            client.processBackgroundOperation(operationAndData, null);
        }

        private String pathInForeground(String path, byte[] data) 
        {
            TimeTrace trace = client.getZookeeperClient().startTracer("CreateBuilderImpl-Foreground");

            AtomicBoolean firstTime = new AtomicBoolean(true);
            String returnPath = RetryLoop.callWithRetry(
            client.getZookeeperClient(),
            new Callable<String>()
            {
                    @Override
                    public String call()
                    {
                        bool localFirstTime = firstTime.getAndSet(false) && !debugForceFindProtectedNode;
                        String createdPath = null;
                        if ( !localFirstTime && doProtected )
                        {
                            debugForceFindProtectedNode = false;
                            createdPath = findProtectedNodeInForeground(path);
                        }

                        if ( createdPath == null )
                        {
                            try
                            {
                                createdPath = client.getZooKeeper().create(path, data, acling.getAclList(path), createMode);
                            }
                            catch (KeeperException.NoNodeException e)
                            {
                                if (createParentsIfNeeded)
                                {
                                    ZKPaths.mkdirs(client.getZooKeeper(), path, false, client.getAclProvider(), createParentsAsContainers);
                                    createdPath = client.getZooKeeper().create(path, data, acling.getAclList(path), createMode);
                                }
                                else
                                {
                                    throw e;
                                }
                            }
                        }

                        if ( failNextCreateForTesting )
                        {
                            failNextCreateForTesting = false;
                            throw new KeeperException.ConnectionLossException();
                        }
                        return createdPath;
                    }
            });
            trace.commit();
            return returnPath;
        }

        private String findProtectedNodeInForeground(String path)
        {
            TimeTrace trace = client.getZookeeperClient().startTracer("CreateBuilderImpl-findProtectedNodeInForeground");

            String returnPath = RetryLoop.callWithRetry
            (
                client.getZookeeperClient(),
                new Callable<String>()
                {
                    @Override
                    public String call()
                    {
                        String foundNode = null;
                        try
                        {
                            final ZKPaths.PathAndNode pathAndNode = ZKPaths.getPathAndNode(path);
                            List<String> children = client.getZooKeeper().getChildren(pathAndNode.getPath(), false);
                            foundNode = findNode(children, pathAndNode.getPath(), protectedId);
                        }
                        catch ( KeeperException.NoNodeException ignore )
                        {
                            // ignore
                        }
                        return foundNode;
                    }
                }
            );

            trace.commit();
            return returnPath;
        }

        internal String adjustPath(String path)
        {
            if ( doProtected )
            {
                ZKPaths.PathAndNode pathAndNode = ZKPaths.getPathAndNode(path);
                String name = getProtectedPrefix(protectedId) + pathAndNode.getNode();
                path = ZKPaths.makePath(pathAndNode.getPath(), name);
            }
            return path;
        }

        /**
         * Attempt to find the znode that matches the given path and protected id
         *
         * @param children    a list of candidates znodes
         * @param path        the path
         * @param protectedId the protected id
         * @return the absolute path of the znode or <code>null</code> if it is not found
         */
        static String findNode(List<String> children, String path, String protectedId)
        {
            String protectedPrefix = getProtectedPrefix(protectedId);
            String foundNode = Iterables.find
            (
                children,
                new Predicate<String>()
                {
                        @Override
                        public boolean apply(String node)
                        {
                            return node.startsWith(protectedPrefix);
                        }
                },
                null
            );
            if ( foundNode != null )
            {
                foundNode = ZKPaths.makePath(path, foundNode);
            }
            return foundNode;
        }
    }
}
