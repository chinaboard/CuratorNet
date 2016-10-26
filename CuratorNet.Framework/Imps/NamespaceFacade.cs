using System;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.CuratorNet.Framework.Listen;
using Org.Apache.CuratorNet.Framework.State;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class NamespaceFacade : CuratorFrameworkImpl
    {
        private readonly CuratorFrameworkImpl client;
        private readonly NamespaceImpl @namespace;
        private readonly FailedDeleteManager failedDeleteManager;

        internal NamespaceFacade(CuratorFrameworkImpl client, string @namespace) 
            : base(client)
        {
            this.client = client;
            this.@namespace = new NamespaceImpl(client, @namespace);
            failedDeleteManager = new FailedDeleteManager(this);
        }

        public CuratorFramework nonNamespaceView()
        {
            return usingNamespace(null);
        }

        public CuratorFramework usingNamespace(String newNamespace)
        {
            return client.getNamespaceFacadeCache().get(newNamespace);
        }

        public String getNamespace()
        {
            return @namespace.getNamespace();
        }

        public void start()
        {
            throw new NotImplementedException();
        }

        public void close()
        {
            throw new NotImplementedException();
        }

        public Listenable<IConnectionStateListener> getConnectionStateListenable()
        {
            return client.getConnectionStateListenable();
        }

        public Listenable<ICuratorListener> getCuratorListenable()
        {
            throw new InvalidOperationException("getCuratorListenable() is only available from a non-namespaced CuratorFramework instance");
        }

        public Listenable<IUnhandledErrorListener> getUnhandledErrorListenable()
        {
            return client.getUnhandledErrorListenable();
        }

        public void sync(String path, Object context)
        {
            internalSync(this, path, context);
        }

        public CuratorZookeeperClient getZookeeperClient()
        {
            return client.getZookeeperClient();
        }

        internal RetryLoop newRetryLoop()
        {
            return client.newRetryLoop();
        }

        internal ZooKeeper getZooKeeper()
        {
            return client.getZooKeeper();
        }

        internal void processBackgroundOperation<DATA_TYPE>(OperationAndData<DATA_TYPE> operationAndData, ICuratorEvent @event)
        {
            client.processBackgroundOperation(operationAndData, @event);
        }

        internal void logError(String reason, Exception e)
        {
            client.logError(reason, e);
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

        public EnsurePath newNamespaceAwareEnsurePath(String path)
        {
            return @namespace.newNamespaceAwareEnsurePath(path);
        }

        internal FailedDeleteManager getFailedDeleteManager()
        {
            return failedDeleteManager;
        }
    }
}