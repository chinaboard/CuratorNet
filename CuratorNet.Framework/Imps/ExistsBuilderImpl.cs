using System;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class ExistsBuilderImpl : IExistsBuilder, IBackgroundOperation<String>, IErrorListenerPathable<Stat>
    {
        private CuratorFrameworkImpl client;
        private Backgrounding backgrounding;
        private Watching watching;
        private bool createParentContainersIfNeeded;

        ExistsBuilderImpl(CuratorFrameworkImpl client)
        {
            this.client = client;
            backgrounding = new Backgrounding();
            watching = new Watching();
            createParentContainersIfNeeded = false;
        }

        public IExistsBuilderMain creatingParentContainersIfNeeded()
        {
            createParentContainersIfNeeded = true;
            return this;
        }

        public IBackgroundPathable<Stat> watched()
        {
            watching = new Watching(true);
            return this;
        }

        public IBackgroundPathable<Stat> usingWatcher(Watcher watcher)
        {
            watching = new Watching(client, watcher);
            return this;
        }

        public IBackgroundPathable<Stat> usingWatcher(CuratorWatcher watcher)
        {
            watching = new Watching(client, watcher);
            return this;
        }

        public IErrorListenerPathable<Stat> inBackground(IBackgroundCallback callback, Object context)
        {
            backgrounding = new Backgrounding(callback, context);
            return this;
        }

        public IErrorListenerPathable<Stat> inBackground(IBackgroundCallback callback, 
                                                            Object context, 
                                                            IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, context, executor);
            return this;
        }

        public IErrorListenerPathable<Stat> inBackground(IBackgroundCallback callback)
        {
            backgrounding = new Backgrounding(callback);
            return this;
        }

        public IErrorListenerPathable<Stat> inBackground(IBackgroundCallback callback, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, executor);
            return this;
        }

        public IErrorListenerPathable<Stat> inBackground()
        {
            backgrounding = new Backgrounding(true);
            return this;
        }

        public IErrorListenerPathable<Stat> inBackground(Object context)
        {
            backgrounding = new Backgrounding(context);
            return this;
        }

        public IPathable<Stat> withUnhandledErrorListener(IUnhandledErrorListener listener)
        {
            backgrounding = new Backgrounding(backgrounding, listener);
            return this;
        }

        public void performBackgroundOperation(OperationAndData<String> operationAndData)
        {
            try
            {
                TimeTrace trace = client.getZookeeperClient().startTracer("ExistsBuilderImpl-Background");
                var callback = new AsyncCallback.StatCallback()
                {
                    @Override
                    public void processResult(int rc, String path, Object ctx, Stat stat)
                    {
                        trace.commit();
                        CuratorEvent @event = new CuratorEventImpl(client, CuratorEventType.EXISTS, rc, path, null, ctx, stat, null, null, null, null);
                        client.processBackgroundOperation(operationAndData, @event);
                    }
                };
                if ( watching.isWatched() )
                {
                    client.getZooKeeper().exists(operationAndData.getData(), true, callback, backgrounding.getContext());
                }
                else
                {
                    client.getZooKeeper().exists(operationAndData.getData(), watching.getWatcher(), callback, backgrounding.getContext());
                }
            }
            catch ( Exception e )
            {
                backgrounding.checkError(e);
            }
        }

        public Stat forPath(String path)
        {
            path = client.fixForNamespace(path);

            Stat returnStat = null;
            if ( backgrounding.inBackground() )
            {
                var operationAndData = new OperationAndData<String>(this, 
                                                                    path, 
                                                                    backgrounding.getCallback(), 
                                                                    null, 
                                                                    backgrounding.getContext());
                if (createParentContainersIfNeeded)
                {
                    CreateBuilderImpl.backgroundCreateParentsThenNode(client, 
                                                                        operationAndData, 
                                                                        operationAndData.getData(),
                                                                        backgrounding, 
                                                                        true);
                }
                else
                {
                    client.processBackgroundOperation(operationAndData, null);
                }
            }
            else
            {
                returnStat = pathInForeground(path);
            }
            return returnStat;
        }

        private Stat pathInForeground(String path)
        {
            if ( createParentContainersIfNeeded )
            {
                String parent = ZKPaths.getPathAndNode(path).getPath();
                if (!parent.Equals(ZKPaths.PATH_SEPARATOR))
                {
                    TimeTrace trace = client.getZookeeperClient().startTracer("ExistsBuilderImpl-Foreground-CreateParents");
                    RetryLoop.callWithRetry
                    (
                        client.getZookeeperClient(),
                        CallableUtils.FromFunc(() =>
                        {
                            try
                            {
                                ZKPaths.mkdirs(client.getZooKeeper(), parent, true, client.getAclProvider(), true);
                            }
                            catch (KeeperException e)
                            {
                                // ignore
                            }
                            return null;
                        })
                    );
                    trace.commit();
                }
            }
            return pathInForegroundStandard(path);
        }

        private Stat pathInForegroundStandard(String path)
        {
            TimeTrace trace = client.getZookeeperClient().startTracer("ExistsBuilderImpl-Foreground");
            Stat returnStat = RetryLoop.callWithRetry
            (
                client.getZookeeperClient(),
                CallableUtils.FromFunc(() =>
                {
                    Stat returnStat;
                    if (watching.isWatched())
                    {
                        returnStat = client.getZooKeeper().exists(path, true);
                    }
                    else
                    {
                        returnStat = client.getZooKeeper().exists(path, watching.getWatcher());
                    }
                    return returnStat;
                })
            );
            trace.commit();
            return returnStat;
        }
    }
}