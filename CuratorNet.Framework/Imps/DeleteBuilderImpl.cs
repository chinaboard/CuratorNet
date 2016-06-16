using System;
using System.CodeDom.Compiler;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.CuratorNet.Framework.API.Transaction;
using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class DeleteBuilderImpl : DeleteBuilder, IBackgroundOperation<String>, IErrorListenerPathable<Object>
    {
        private readonly CuratorFrameworkImpl client;
        private int version;
        private Backgrounding backgrounding;
        private bool deletingChildrenIfNeeded;
        private bool isGuaranteed;

        DeleteBuilderImpl(CuratorFrameworkImpl client)
        {
            this.client = client;
            version = -1;
            backgrounding = new Backgrounding();
            deletingChildrenIfNeeded = false;
            isGuaranteed = false;
        }

        private class TransactionDeleteBuilder : ITransactionDeleteBuilder<ICuratorTransactionBridge>
        {
            private readonly DeleteBuilderImpl _deleteBuilderImpl;
            private readonly CuratorTransactionImpl _curatorTransaction;
            private readonly CuratorMultiTransactionRecord _transaction;

            public TransactionDeleteBuilder(DeleteBuilderImpl deleteBuilderImpl, 
                                                CuratorTransactionImpl curatorTransaction,
                                                CuratorMultiTransactionRecord transaction)
            {
                _deleteBuilderImpl = deleteBuilderImpl;
                _curatorTransaction = curatorTransaction;
                _transaction = transaction;
            }

            public ICuratorTransactionBridge forPath(String path)
            {
                String fixedPath = _deleteBuilderImpl.client.fixForNamespace(path);
                Op delete = Op.delete(fixedPath, _deleteBuilderImpl.version);
                _transaction.add(delete, OperationType.DELETE, path);
                return _curatorTransaction;
            }

            public IPathable<ICuratorTransactionBridge> withVersion(int version)
            {
                _deleteBuilderImpl.withVersion(version);
                return this;
            }
        }

        ITransactionDeleteBuilder asTransactionDeleteBuilder(CuratorTransactionImpl curatorTransaction, 
                                                                CuratorMultiTransactionRecord transaction)
        {
            return new TransactionDeleteBuilder(this);
        }

        public IChildrenDeletable guaranteed()
        {
            isGuaranteed = true;
            return this;
        }

        public IBackgroundVersionable deletingChildrenIfNeeded()
        {
            deletingChildrenIfNeeded = true;
            return this;
        }

        public IBackgroundPathable<object> withVersion(int version)
        {
            this.version = version;
            return this;
        }

        public IErrorListenerPathable<object> inBackground(IBackgroundCallback callback, Object context)
        {
            backgrounding = new Backgrounding(callback, context);
            return this;
        }

        public IErrorListenerPathable<object> inBackground(IBackgroundCallback callback, Object context, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, context, executor);
            return this;
        }

        public IErrorListenerPathable<object> inBackground(IBackgroundCallback callback)
        {
            backgrounding = new Backgrounding(callback);
            return this;
        }

        public IErrorListenerPathable<object> inBackground(IBackgroundCallback callback, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, executor);
            return this;
        }

        public IErrorListenerPathable<object> inBackground()
        {
            backgrounding = new Backgrounding(true);
            return this;
        }

        public IErrorListenerPathable<object> inBackground(Object context)
        {
            backgrounding = new Backgrounding(context);
            return this;
        }

        public IPathable<Object> withUnhandledErrorListener(IUnhandledErrorListener listener)
        {
            backgrounding = new Backgrounding(backgrounding, listener);
            return this;
        }

        public void performBackgroundOperation(OperationAndData<String> operationAndData)
        {
            try
            {
                TimeTrace trace = client.getZookeeperClient().startTracer("DeleteBuilderImpl-Background");
                client.getZooKeeper().delete
                (
                    operationAndData.getData(),
                    version,
                    new AsyncCallback.VoidCallback()
                    {
                            @Override
                            public void processResult(int rc, String path, Object ctx)
                            {
                                trace.commit();
                                if ((rc == KeeperException.Code.NOTEMPTY.intValue()) && deletingChildrenIfNeeded)
                                {
                                    backgroundDeleteChildrenThenNode(operationAndData);
                                }
                                else
                                {
                                    CuratorEvent @event = new CuratorEventImpl(client, 
                                                                                CuratorEventType.DELETE, 
                                                                                rc, 
                                                                                path, 
                                                                                null, 
                                                                                ctx,
                                                                                null, 
                                                                                null,
                                                                                null, 
                                                                                null, 
                                                                                null);
                                    client.processBackgroundOperation(operationAndData, event);
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

        private void backgroundDeleteChildrenThenNode(OperationAndData<String> mainOperationAndData)
        {
            IBackgroundOperation<String> operation = new BackgroundOperation<String>()
            {
                public void performBackgroundOperation(OperationAndData<String> dummy) throws Exception
                {
                                try
                                {
                        ZKPaths.deleteChildren(client.getZooKeeper(), mainOperationAndData.getData(), false);
                    }
                                catch ( KeeperException e )
                                {
                        // ignore
                    }
                    client.queueOperation(mainOperationAndData);
                }
            };
            var parentOperation = new OperationAndData<String>(operation, mainOperationAndData.getData(), null, null, backgrounding.getContext());
            client.queueOperation(parentOperation);
        }

        public Void forPath(String path)
        {
            String unfixedPath = path;
            path = client.fixForNamespace(path);

            if ( backgrounding.inBackground() )
            {
                OperationAndData.ErrorCallback<String> errorCallback = null;
                if (guaranteed)
                {
                    errorCallback = new OperationAndData.ErrorCallback<String>()
                    {
                        public void retriesExhausted(OperationAndData<String> operationAndData)
                        {
                            client.getFailedDeleteManager().addFailedDelete(unfixedPath);
                        }
                    };
                }
                client.processBackgroundOperation(new OperationAndData<String>(this, path, backgrounding.getCallback(), errorCallback, backgrounding.getContext()), null);
            }
            else
            {
                pathInForeground(path, unfixedPath);
            }
            return null;
        }

        protected int getVersion()
        {
            return version;
        }

        private void pathInForeground(String path, String unfixedPath) 
        {
            TimeTrace trace = client.getZookeeperClient().startTracer("DeleteBuilderImpl-Foreground");
            try
            {
                RetryLoop.callWithRetry
                (
                    client.getZookeeperClient(),
                    new Callable<Void>()
                    {
                        @Override
                        public Void call() throws Exception
                        {
                            try
                            {
                                client.getZooKeeper().delete(path, version);
                            }
                            catch ( KeeperException.NotEmptyException e )
                            {
                                if (deletingChildrenIfNeeded)
                                {
                                    ZKPaths.deleteChildren(client.getZooKeeper(), path, true);
                                }
                                else
                                {
                                    throw e;
                                }
                            }
                            return null;
                        }
                    }
                );
            }
            catch ( Exception e )
            {
                ThreadUtils.checkInterrupted(e);
                //Only retry a guaranteed delete if it's a retryable error
                if( (RetryLoop.isRetryException(e)) && guaranteed)
                {
                    client.getFailedDeleteManager().addFailedDelete(unfixedPath);
                }
                throw e;
            }
            trace.commit();
        }
    }
}