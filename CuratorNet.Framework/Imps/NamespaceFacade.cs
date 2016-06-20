using System;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.CuratorNet.Framework.API;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class NamespaceFacade : CuratorFrameworkImpl
    {
        private readonly CuratorFrameworkImpl client;
        private readonly NamespaceImpl @namespace;
        private readonly FailedDeleteManager failedDeleteManager = new FailedDeleteManager(this);

        NamespaceFacade(CuratorFrameworkImpl client, string @namespace)
        {
            base(client);
            this.client = client;
            this.@namespace = new NamespaceImpl(client, @namespace);
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

        public Listenable<ConnectionStateListener> getConnectionStateListenable()
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

        RetryLoop newRetryLoop()
        {
            return client.newRetryLoop();
        }

        ZooKeeper getZooKeeper()
        {
            return client.getZooKeeper();
        }

        void processBackgroundOperation<DATA_TYPE>(OperationAndData<DATA_TYPE> operationAndData, ICuratorEvent @event)
        {
            client.processBackgroundOperation(operationAndData, @event);
        }

        void logError(String reason, Exception e)
        {
            client.logError(reason, e);
        }

        String unfixForNamespace(String path)
        {
            return @namespace.unfixForNamespace(path);
        }

        String fixForNamespace(String path)
        {
            return @namespace.fixForNamespace(path, false);
        }

        String fixForNamespace(String path, bool isSequential)
        {
            return @namespace.fixForNamespace(path, isSequential);
        }

        public EnsurePath newNamespaceAwareEnsurePath(String path)
        {
            return @namespace.newNamespaceAwareEnsurePath(path);
        }

        FailedDeleteManager getFailedDeleteManager()
        {
            return failedDeleteManager;
        }
    }
}