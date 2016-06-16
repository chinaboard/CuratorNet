using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    class GetChildrenBuilderImpl : IGetChildrenBuilder, IBackgroundOperation<String>, IErrorListenerPathable<List<String>>
    {
        private CuratorFrameworkImpl client;
        private Watching watching;
        private Backgrounding backgrounding;
        private Stat responseStat;

        GetChildrenBuilderImpl(CuratorFrameworkImpl client)
        {
            this.client = client;
            watching = new Watching();
            backgrounding = new Backgrounding();
            responseStat = null;
        }

        public IWatchPathable<List<String>> storingStatIn(Stat stat)
        {
            responseStat = stat;
            return new WatchPathable<List<String>>()
            {
                @Override
                public List<String> forPath(String path) throws Exception
                {
                    return GetChildrenBuilderImpl.this.forPath(path);
                }

                @Override
                        public Pathable<List<String>> watched()
                {
                    GetChildrenBuilderImpl.this.watched();
                    return GetChildrenBuilderImpl.this;
                }

                @Override
                        public Pathable<List<String>> usingWatcher(Watcher watcher)
                {
                    GetChildrenBuilderImpl.this.usingWatcher(watcher);
                    return GetChildrenBuilderImpl.this;
                }

                @Override
                        public Pathable<List<String>> usingWatcher(CuratorWatcher watcher)
                {
                    GetChildrenBuilderImpl.this.usingWatcher(watcher);
                    return GetChildrenBuilderImpl.this;
                }
            };
        }

        public IErrorListenerPathable<List<String>> inBackground(IBackgroundCallback callback, Object context)
        {
            backgrounding = new Backgrounding(callback, context);
            return this;
        }

        public IErrorListenerPathable<List<String>> inBackground(IBackgroundCallback callback, Object context, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, context, executor);
            return this;
        }

        public IErrorListenerPathable<List<String>> inBackground(IBackgroundCallback callback)
        {
            backgrounding = new Backgrounding(callback);
            return this;
        }

        public IErrorListenerPathable<List<String>> inBackground(IBackgroundCallback callback, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, executor);
            return this;
        }

        public IErrorListenerPathable<List<String>> inBackground()
        {
            backgrounding = new Backgrounding(true);
            return this;
        }

        public IErrorListenerPathable<List<String>> inBackground(Object context)
        {
            backgrounding = new Backgrounding(context);
            return this;
        }

        public IPathable<List<String>> withUnhandledErrorListener(IUnhandledErrorListener listener)
        {
            backgrounding = new Backgrounding(backgrounding, listener);
            return this;
        }

        public IBackgroundPathable<List<String>> watched()
        {
            watching = new Watching(true);
            return this;
        }

        public IBackgroundPathable<List<String>> usingWatcher(Watcher watcher)
        {
            watching = new Watching(client, watcher);
            return this;
        }

        public IBackgroundPathable<List<String>> usingWatcher(CuratorWatcher watcher)
        {
            watching = new Watching(client, watcher);
            return this;
        }

    public void performBackgroundOperation(final OperationAndData<String> operationAndData) 
    {
        try
        {
            TimeTrace trace = client.getZookeeperClient().startTracer("GetChildrenBuilderImpl-Background");
            var callback = new AsyncCallback.Children2Callback()
            {
                public void processResult(int rc, String path, Object o, List<String> strings, Stat stat)
                {
                    trace.commit();
                    if (strings == null)
                    {
                        strings = Lists.newArrayList();
                    }
                    CuratorEventImpl event = new CuratorEventImpl(client, CuratorEventType.CHILDREN, rc, path, null, o, stat, null, strings, null, null);
                    client.processBackgroundOperation(operationAndData, event);
                }
            };
            if ( watching.isWatched() )
            {
                client.getZooKeeper().getChildren(operationAndData.getData(), true, callback, backgrounding.getContext());
            }
            else
            {
                client.getZooKeeper().getChildren(operationAndData.getData(), watching.getWatcher(), callback, backgrounding.getContext());
            }
        }
        catch ( Exception e )
        {
            backgrounding.checkError(e);
        }
    }

    public List<String> forPath(String path)
    {
        path = client.fixForNamespace(path);
        List<String> children = null;
        if ( backgrounding.inBackground() )
        {
            client.processBackgroundOperation(new OperationAndData<String>(this, path, backgrounding.getCallback(), null, backgrounding.getContext()), null);
        }
        else
        {
            children = pathInForeground(path);
        }
        return children;
    }

        private List<String> pathInForeground(String path)
        {
            TimeTrace trace = client.getZookeeperClient().startTracer("GetChildrenBuilderImpl-Foreground");
            List<String>    children = RetryLoop.callWithRetry
            (
                client.getZookeeperClient(),
                new Callable<List<String>>()
                {
                    @Override
                    public List<String> call() throws Exception
                    {
                        List<String>    children;
                                        if ( watching.isWatched() )
                                        {
                            children = client.getZooKeeper().getChildren(path, true, responseStat);
                        }
                                        else
                                        {
                            children = client.getZooKeeper().getChildren(path, watching.getWatcher(), responseStat);
                        }
                                        return children;
                    }
                }
            );
            trace.commit();
            return children;
        }
    }
}