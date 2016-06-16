using System;
using System.Collections.Generic;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class GetACLBuilderImpl : IGetACLBuilder, IBackgroundOperation<String>, IErrorListenerPathable<List<ACL>>
    {
        private CuratorFrameworkImpl client;

        private Backgrounding backgrounding;
        private Stat responseStat;

        GetACLBuilderImpl(CuratorFrameworkImpl client)
        {
            this.client = client;
            backgrounding = new Backgrounding();
            responseStat = new Stat();
        }

        public IErrorListenerPathable<List<ACL>> inBackground(IBackgroundCallback callback, Object context)
        {
            backgrounding = new Backgrounding(callback, context);
            return this;
        }

        public IErrorListenerPathable<List<ACL>> inBackground(IBackgroundCallback callback, Object context, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, context, executor);
            return this;
        }

        public IErrorListenerPathable<List<ACL>> inBackground()
        {
            backgrounding = new Backgrounding(true);
            return this;
        }

        public IErrorListenerPathable<List<ACL>> inBackground(Object context)
        {
            backgrounding = new Backgrounding(context);
            return this;
        }

        public IErrorListenerPathable<List<ACL>> inBackground(IBackgroundCallback callback)
        {
            backgrounding = new Backgrounding(callback);
            return this;
        }

        public IErrorListenerPathable<List<ACL>> inBackground(IBackgroundCallback callback, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, executor);
            return this;
        }

        public IPathable<List<ACL>> withUnhandledErrorListener(IUnhandledErrorListener listener)
        {
            backgrounding = new Backgrounding(backgrounding, listener);
            return this;
        }

        public IPathable<List<ACL>> storingStatIn(Stat stat)
        {
            responseStat = stat;
            return this;
        }

        public void performBackgroundOperation(OperationAndData<String> operationAndData) 
        {
            try
            {
                TimeTrace trace = client.getZookeeperClient().startTracer("GetACLBuilderImpl-Background");
                var callback = new AsyncCallback.ACLCallback()
                {
                    @Override
                    public void processResult(int rc, String path, Object ctx, List<ACL> acl, Stat stat)
                    {
                        trace.commit();
                        CuratorEventImpl event = new CuratorEventImpl(client, CuratorEventType.GET_ACL, rc, path, null, ctx, stat, null, null, null, acl);
                        client.processBackgroundOperation(operationAndData, event);
                    }
                };
                client.getZooKeeper().getACL(operationAndData.getData(), responseStat, callback, backgrounding.getContext());
            }
            catch ( Exception e )
            {
                backgrounding.checkError(e);
            }
        }

        public List<ACL> forPath(String path)
        {
            path = client.fixForNamespace(path);

            List<ACL>       result = null;
            if ( backgrounding.inBackground() )
            {
                client.processBackgroundOperation(new OperationAndData<String>(this, path, backgrounding.getCallback(), null, backgrounding.getContext()), null);
            }
            else
            {
                result = pathInForeground(path);
            }
            return result;
        }

        private List<ACL> pathInForeground(String path)
        {
            TimeTrace trace = client.getZookeeperClient().startTracer("GetACLBuilderImpl-Foreground");
            List<ACL> result = RetryLoop.callWithRetry
            (
                client.getZookeeperClient(),
                CallableUtils.FromFunc<List<ACL>>(() => client.getZooKeeper().getACL(path, responseStat))
            );
            trace.commit();
            return result;
        }
    }
}