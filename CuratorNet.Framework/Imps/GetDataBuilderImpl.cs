using System;
using System.CodeDom.Compiler;
using NLog;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    public class GetDataBuilderImpl : GetDataBuilder, IBackgroundOperation<String>, IErrorListenerPathable<byte[]>
    {
        private Logger                log = LogManager.GetCurrentClassLogger();
        private CuratorFrameworkImpl  client;
        private Stat responseStat;
        private Watching watching;
        private Backgrounding backgrounding;
        private bool decompress;

        GetDataBuilderImpl(CuratorFrameworkImpl client)
        {
            this.client = client;
            responseStat = null;
            watching = new Watching();
            backgrounding = new Backgrounding();
            decompress = false;
        }

        private class DecompressGetDataWatchBackgroundStatable : IGetDataWatchBackgroundStatable
        {
            private readonly GetDataBuilderImpl _getDataBuilderImpl;

            public DecompressGetDataWatchBackgroundStatable(GetDataBuilderImpl getDataBuilderImpl)
            {
                _getDataBuilderImpl = getDataBuilderImpl;
            }

            public IErrorListenerPathable<byte[]> inBackground()
            {
                return _getDataBuilderImpl.inBackground();
            }

            public IErrorListenerPathable<byte[]> inBackground(IBackgroundCallback callback, Object context)
            {
                return _getDataBuilderImpl.inBackground(callback, context);
            }

            public IErrorListenerPathable<byte[]> inBackground(IBackgroundCallback callback, Object context, IExecutor executor)
            {
                return _getDataBuilderImpl.inBackground(callback, context, executor);
            }

            public IErrorListenerPathable<byte[]> inBackground(Object context)
            {
                return _getDataBuilderImpl.inBackground(context);
            }

            public IErrorListenerPathable<byte[]> inBackground(IBackgroundCallback callback)
            {
                return _getDataBuilderImpl.inBackground(callback);
            }

            public IErrorListenerPathable<byte[]> inBackground(IBackgroundCallback callback, IExecutor executor)
            {
                return _getDataBuilderImpl.inBackground(callback, executor);
            }

            public byte[] forPath(String path)
            {
                return _getDataBuilderImpl.forPath(path);
            }

            public IWatchPathable<byte[]> storingStatIn(Stat stat)
            {
                return _getDataBuilderImpl.storingStatIn(stat);
            }

            public IBackgroundPathable<byte[]> watched()
            {
                return _getDataBuilderImpl.watched();
            }

            public IBackgroundPathable<byte[]> usingWatcher(Watcher watcher)
            {
                return _getDataBuilderImpl.usingWatcher(watcher);
            }

            public IBackgroundPathable<byte[]> usingWatcher(CuratorWatcher watcher)
            {
                return _getDataBuilderImpl.usingWatcher(watcher);
            }
        }

        public IGetDataWatchBackgroundStatable decompressed()
        {
            decompress = true;
            return new DecompressGetDataWatchBackgroundStatable(this);
        }

        private class StoringStatsWatchPathable : IWatchPathable<byte[]>
        {
            private readonly GetDataBuilderImpl _getDataBuilderImpl;

            public StoringStatsWatchPathable(GetDataBuilderImpl getDataBuilderImpl)
            {
                _getDataBuilderImpl = getDataBuilderImpl;
            }

            public byte[] forPath(String path)
            {
                return _getDataBuilderImpl.forPath(path);
            }

            public IPathable<byte[]> watched()
            {
                _getDataBuilderImpl.watched();
                return _getDataBuilderImpl;
            }

            public IPathable<byte[]> usingWatcher(Watcher watcher)
            {
                _getDataBuilderImpl.usingWatcher(watcher);
                return _getDataBuilderImpl;
            }

            public IPathable<byte[]> usingWatcher(CuratorWatcher watcher)
            {
                _getDataBuilderImpl.usingWatcher(watcher);
                return _getDataBuilderImpl;
            }
        }

        public IWatchPathable<byte[]> storingStatIn(Stat stat)
        {
            this.responseStat = stat;
            return new StoringStatsWatchPathable(this);
        }

        public IErrorListenerPathable<byte[]> inBackground(IBackgroundCallback callback, Object context)
        {
            backgrounding = new Backgrounding(callback, context);
            return this;
        }

        public IErrorListenerPathable<byte[]> inBackground(IBackgroundCallback callback, Object context, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, context, executor);
            return this;
        }

        public IErrorListenerPathable<byte[]> inBackground(IBackgroundCallback callback)
        {
            backgrounding = new Backgrounding(callback);
            return this;
        }

        public IErrorListenerPathable<byte[]> inBackground(IBackgroundCallback callback, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, executor);
            return this;
        }

        public IErrorListenerPathable<byte[]> inBackground()
        {
            backgrounding = new Backgrounding(true);
            return this;
        }

        public IErrorListenerPathable<byte[]> inBackground(Object context)
        {
            backgrounding = new Backgrounding(context);
            return this;
        }

        public IPathable<byte[]> withUnhandledErrorListener(IUnhandledErrorListener listener)
        {
            backgrounding = new Backgrounding(backgrounding, listener);
            return this;
        }

        public IBackgroundPathable<byte[]> watched()
        {
            watching = new Watching(true);
            return this;
        }

        public IBackgroundPathable<byte[]> usingWatcher(Watcher watcher)
        {
            watching = new Watching(client, watcher);
            return this;
        }

        public IBackgroundPathable<byte[]> usingWatcher(CuratorWatcher watcher)
        {
            watching = new Watching(client, watcher);
            return this;
        }

        public void performBackgroundOperation(OperationAndData<String> operationAndData) 
        {
            try
            {
                TimeTrace   trace = client.getZookeeperClient().startTracer("GetDataBuilderImpl-Background");
                AsyncCallback.DataCallback callback = new AsyncCallback.DataCallback()
                {
                    @Override
                    public void processResult(int rc, String path, Object ctx, byte[] data, Stat stat)
                    {
                        trace.commit();
                        if (decompress && (data != null))
                        {
                            try
                            {
                                data = client.getCompressionProvider().decompress(path, data);
                            }
                            catch (Exception e)
                            {
                                ThreadUtils.checkInterrupted(e);
                                log.error("Decompressing for path: " + path, e);
                                rc = KeeperException.Code.DATAINCONSISTENCY.intValue();
                            }
                        }
                        CuratorEvent event = new CuratorEventImpl(client, CuratorEventType.GET_DATA, rc, path, null, ctx, stat, data, null, null, null);
                        client.processBackgroundOperation(operationAndData, event);
                    }
                };
                if ( watching.isWatched() )
                {
                    client.getZooKeeper().getData(operationAndData.getData(), true, callback, backgrounding.getContext());
                }
                else
                {
                    client.getZooKeeper().getData(operationAndData.getData(), watching.getWatcher(), callback, backgrounding.getContext());
                }
            }
            catch ( Exception e )
            {
                backgrounding.checkError(e);
            }
        }

        public byte[] forPath(String path)
        {
            path = client.fixForNamespace(path);
            byte[] responseData = null;
            if ( backgrounding.inBackground() )
            {
                client.processBackgroundOperation(new OperationAndData<String>(this, path, backgrounding.getCallback(), null, backgrounding.getContext()), null);
            }
            else
            {
                responseData = pathInForeground(path);
            }
            return responseData;
        }

        private byte[] pathInForeground(String path)
        {
            TimeTrace trace = client.getZookeeperClient().startTracer("GetDataBuilderImpl-Foreground");
            byte[] responseData = RetryLoop.callWithRetry
            (
                client.getZookeeperClient(),
                CallableUtils.FromFunc(() =>
                {
                    byte[] responseData;
                    if (watching.isWatched())
                    {
                        responseData = client.getZooKeeper().getData(path, true, responseStat);
                    }
                    else
                    {
                        responseData = client.getZooKeeper().getData(path, watching.getWatcher(), responseStat);
                    }
                    return responseData;
                })
            );
            trace.commit();
            return decompress ? client.getCompressionProvider().decompress(path, responseData) : responseData;
        }
    }
}