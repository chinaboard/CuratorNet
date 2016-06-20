using System;
using System.CodeDom.Compiler;
using System.Threading.Tasks;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.CuratorNet.Framework.API.Transaction;
using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class SetDataBuilderImpl : ISetDataBuilder, IBackgroundOperation<PathAndBytes>, ErrorListenerPathAndBytesable<Stat>
    {
        private readonly CuratorFrameworkImpl client;
        private Backgrounding backgrounding;
        private int version;
        private bool compress;

        SetDataBuilderImpl(CuratorFrameworkImpl client)
        {
            this.client = client;
            backgrounding = new Backgrounding();
            version = -1;
            compress = false;
        }

        private class TransactionITransactionSetDataBuilder : ITransactionSetDataBuilder
        {
            private readonly SetDataBuilderImpl _setDataBuilderImpl;
            private readonly CuratorMultiTransactionRecord _transaction;
            private readonly CuratorTransactionImpl _curatorTransaction;

            public TransactionITransactionSetDataBuilder(SetDataBuilderImpl setDataBuilderImpl, 
                                                            CuratorMultiTransactionRecord transaction, 
                                                            CuratorTransactionImpl curatorTransaction)
            {
                _setDataBuilderImpl = setDataBuilderImpl;
                _transaction = transaction;
                _curatorTransaction = curatorTransaction;
            }

            public ICuratorTransactionBridge forPath(String path, byte[] data)
            {
                if (_setDataBuilderImpl.compress)
                {
                    data = _setDataBuilderImpl.client.getCompressionProvider().compress(path, data);
                }
                String fixedPath = _setDataBuilderImpl.client.fixForNamespace(path);
                _transaction.add(Op.setData(fixedPath, data, _setDataBuilderImpl.version), 
                                    OperationType.SET_DATA, 
                                    path);
                return _curatorTransaction;
            }

            public ICuratorTransactionBridge forPath(String path)
            {
                return forPath(path, _setDataBuilderImpl.client.getDefaultData());
            }

            public PathAndBytesable<ICuratorTransactionBridge> withVersion(int version)
            {
                _setDataBuilderImpl.withVersion(version);
                return this;
            }

            public IVersionPathAndBytesable<ICuratorTransactionBridge> compressed()
            {
                _setDataBuilderImpl.compress = true;
                return this;
            }
        }

        internal ITransactionSetDataBuilder asTransactionSetDataBuilder(CuratorTransactionImpl curatorTransaction, 
                                                                            CuratorMultiTransactionRecord transaction)
        {
            return new TransactionITransactionSetDataBuilder(this, transaction, curatorTransaction);
        }

        private class CompressedSetDataBackgroundVersionable : ISetDataBackgroundVersionable
        {
            private readonly SetDataBuilderImpl _setDataBuilderImpl;

            public CompressedSetDataBackgroundVersionable(SetDataBuilderImpl setDataBuilderImpl)
            {
                _setDataBuilderImpl = setDataBuilderImpl;
            }

            public ErrorListenerPathAndBytesable<Stat> inBackground()
            {
                return _setDataBuilderImpl.inBackground();
            }

            public ErrorListenerPathAndBytesable<Stat> inBackground(IBackgroundCallback callback, Object context)
            {
                return _setDataBuilderImpl.inBackground(callback, context);
            }

            public ErrorListenerPathAndBytesable<Stat> inBackground(IBackgroundCallback callback, Object context, IExecutor executor)
            {
                return _setDataBuilderImpl.inBackground(callback, context, executor);
            }

            public ErrorListenerPathAndBytesable<Stat> inBackground(Object context)
            {
                return _setDataBuilderImpl.inBackground(context);
            }

            public ErrorListenerPathAndBytesable<Stat> inBackground(IBackgroundCallback callback)
            {
                return _setDataBuilderImpl.inBackground(callback);
            }

            public ErrorListenerPathAndBytesable<Stat> inBackground(IBackgroundCallback callback, IExecutor executor)
            {
                return _setDataBuilderImpl.inBackground(callback, executor);
            }

            public Stat forPath(String path, byte[] data)
            {
                return _setDataBuilderImpl.forPath(path, data);
            }

            public Stat forPath(String path)
            {
                return _setDataBuilderImpl.forPath(path);
            }

            public IBackgroundPathAndBytesable<Stat> withVersion(int version)
            {
                return _setDataBuilderImpl.withVersion(version);
            }
        }

        public ISetDataBackgroundVersionable compressed()
        {
            compress = true;
            return new CompressedSetDataBackgroundVersionable(this);
        }

        public IBackgroundPathAndBytesable<Stat> withVersion(int version)
        {
            this.version = version;
            return this;
        }

        public ErrorListenerPathAndBytesable<Stat> inBackground(IBackgroundCallback callback, Object context)
        {
            backgrounding = new Backgrounding(callback, context);
            return this;
        }

        public ErrorListenerPathAndBytesable<Stat> inBackground(IBackgroundCallback callback, Object context, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, context, executor);
            return this;
        }

        public ErrorListenerPathAndBytesable<Stat> inBackground(IBackgroundCallback callback)
        {
            backgrounding = new Backgrounding(callback);
            return this;
        }

        public ErrorListenerPathAndBytesable<Stat> inBackground()
        {
            backgrounding = new Backgrounding(true);
            return this;
        }

        public ErrorListenerPathAndBytesable<Stat> inBackground(Object context)
        {
            backgrounding = new Backgrounding(context);
            return this;
        }

        public ErrorListenerPathAndBytesable<Stat> inBackground(IBackgroundCallback callback, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, executor);
            return this;
        }

        public PathAndBytesable<Stat> withUnhandledErrorListener(IUnhandledErrorListener listener)
        {
            backgrounding = new Backgrounding(backgrounding, listener);
            return this;
        }

        public void performBackgroundOperation(OperationAndData<PathAndBytes> operationAndData)
        {
            try
            {
                TimeTrace trace = client.getZookeeperClient().startTracer("SetDataBuilderImpl-Background");
                Task<Stat> task = client.getZooKeeper().setDataAsync
                (
                     operationAndData.getData().getPath(),
                     operationAndData.getData().getData(),
                     version,
                     new AsyncCallback.StatCallback()
                     {
                         public void processResult(int rc, String path, Object ctx, Stat stat)
                         {
                             trace.commit();
                             ICuratorEvent @event = new CuratorEventImpl(client, CuratorEventType.SET_DATA, rc, path, null, ctx, stat, null, null, null, null);
                             client.processBackgroundOperation(operationAndData, @event);
                         };
                    },
                    backgrounding.getContext()
                );
                task.Wait();
            }
            catch ( Exception e )
            {
                backgrounding.checkError(e);
            }
        }

        public Stat forPath(String path)
        {
            return forPath(path, client.getDefaultData());
        }

        public Stat forPath(String path, byte[] data)
        {
            if ( compress )
            {
                data = client.getCompressionProvider().compress(path, data);
            }

            path = client.fixForNamespace(path);

            Stat resultStat = null;
            if ( backgrounding.inBackground()  )
            {
                client.processBackgroundOperation(new OperationAndData<PathAndBytes>(this, new PathAndBytes(path, data), backgrounding.getCallback(), null, backgrounding.getContext()), null);
            }
            else
            {
                resultStat = pathInForeground(path, data);
            }
            return resultStat;
        }

        internal int getVersion()
        {
            return version;
        }

        private Stat pathInForeground(string path, byte[] data)
        {
            TimeTrace trace = client.getZookeeperClient().startTracer("SetDataBuilderImpl-Foreground");
            Stat resultStat = RetryLoop.callWithRetry
            (
                client.getZookeeperClient(),
                CallableUtils.FromFunc(() =>
                {
                    Task<Stat> task = client.getZooKeeper().setDataAsync(path, data, version);
                    task.Wait();
                    return task.Result;
                })
            );
            trace.commit();
            return resultStat;
        }
    }
}