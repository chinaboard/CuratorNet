using System;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    public class SyncBuilderImpl : ISyncBuilder, IBackgroundOperation<string>, IErrorListenerPathable<object>
    {
        private readonly CuratorFrameworkImpl client;
        private Backgrounding backgrounding = new Backgrounding();

        public SyncBuilderImpl(CuratorFrameworkImpl client)
        {
            //To change body of created methods use File | Settings | File Templates.
            this.client = client;
        }

        public IErrorListenerPathable<object> inBackground()
        {
            // NOP always in background
            return this;
        }

        public IErrorListenerPathable<object> inBackground(object context)
        {
            backgrounding = new Backgrounding(context);
            return this;
        }

        public IErrorListenerPathable<object> inBackground(IBackgroundCallback callback)
        {
            backgrounding = new Backgrounding(callback);
            return this;
        }

        public IErrorListenerPathable<object> inBackground(IBackgroundCallback callback, object context)
        {
            backgrounding = new Backgrounding(callback, context);
            return this;
        }

        public IErrorListenerPathable<object> inBackground(IBackgroundCallback callback, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, executor);
            return this;
        }

        public IErrorListenerPathable<object> inBackground(IBackgroundCallback callback, Object context, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, context, executor);
            return this;
        }

        public IPathable<object> withUnhandledErrorListener(IUnhandledErrorListener listener)
        {
            backgrounding = new Backgrounding(backgrounding, listener);
            return this;
        }

        public void performBackgroundOperation(OperationAndData<String> operationAndData)
        {
            try
            {
                TimeTrace trace = client.getZookeeperClient().startTracer("SyncBuilderImpl-Background");
                String path = operationAndData.getData();
                String adjustedPath = client.fixForNamespace(path);

                AsyncCallback.VoidCallback voidCallback = new AsyncCallback.VoidCallback()
                {
                    public void processResult(int rc, String path, Object ctx)
                    {
                        trace.commit();
                        CuratorEvent event = new CuratorEventImpl(client, CuratorEventType.SYNC, rc, path, path, ctx, null, null, null, null, null);
                        client.processBackgroundOperation(operationAndData, event);
                    }
                };
    client.getZooKeeper().sync(adjustedPath, voidCallback, backgrounding.getContext());
            }
            catch ( Exception e )
            {
                backgrounding.checkError(e);
            }
        }

        public object forPath(String path)
        {
            var operationAndData = new OperationAndData<String>(this, path, backgrounding.getCallback(), null, backgrounding.getContext());
            client.processBackgroundOperation(operationAndData, null);
            return null;
        }
    }
}