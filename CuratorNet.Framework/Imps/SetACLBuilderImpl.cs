using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Threading.Tasks;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.CuratorNet.Framework.Imps;
using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class SetACLBuilderImpl : ISetACLBuilder, IBackgroundPathable<Stat>, IBackgroundOperation<String>, IErrorListenerPathable<Stat>
    {
        private readonly CuratorFrameworkImpl client;

        private ACLing acling;
        private Backgrounding backgrounding;
        private int version;

        SetACLBuilderImpl(CuratorFrameworkImpl client)
        {
            this.client = client;
            backgrounding = new Backgrounding();
            acling = new ACLing(client.getAclProvider());
            version = -1;
        }

        public IBackgroundPathable<Stat> withACL(List<ACL> aclList)
        {
            acling = new ACLing(client.getAclProvider(), aclList);
            return this;
        }

        public IACLable<IBackgroundPathable<Stat>> withVersion(int version)
        {
            this.version = version;
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

        public IErrorListenerPathable<Stat> inBackground(IBackgroundCallback callback)
        {
            backgrounding = new Backgrounding(callback);
            return this;
        }

        public IErrorListenerPathable<Stat> inBackground(IBackgroundCallback callback, Object context)
        {
            backgrounding = new Backgrounding(callback, context);
            return this;
        }

        public IErrorListenerPathable<Stat> inBackground(IBackgroundCallback callback, Object context, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, context, executor);
            return this;
        }

        public IErrorListenerPathable<Stat> inBackground(IBackgroundCallback callback, IExecutor executor)
        {
            backgrounding = new Backgrounding(client, callback, executor);
            return this;
        }

        public IPathable<Stat> withUnhandledErrorListener(IUnhandledErrorListener listener)
        {
            backgrounding = new Backgrounding(backgrounding, listener);
            return this;
        }

        public Stat forPath(String path)
        {
            path = client.fixForNamespace(path);

            Stat resultStat = null;
            if ( backgrounding.inBackground()  )
            {
                client.processBackgroundOperation(new OperationAndData<String>(this, path, backgrounding.getCallback(), null, backgrounding.getContext()), null);
            }
            else
            {
                resultStat = pathInForeground(path);
            }
            return resultStat;
        }

        public void performBackgroundOperation(OperationAndData<String> operationAndData)
        {
            try
            {
                TimeTrace trace = client.getZookeeperClient().startTracer("SetACLBuilderImpl-Background");
                String path = operationAndData.getData();
                client.getZooKeeper().setACL
                (
                    path,
                    acling.getAclList(path),
                    version,
                    new AsyncCallback.StatCallback()
                    {
                        public void processResult(int rc, String path, Object ctx, Stat stat)
                        {
                            trace.commit();
                            CuratorEvent event = new CuratorEventImpl(client, CuratorEventType.SET_ACL, rc, path, null, ctx, stat, null, null, null, null);
                            client.processBackgroundOperation(operationAndData, event);
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

        private Stat pathInForeground(string path)
        {
            TimeTrace trace = client.getZookeeperClient().startTracer("SetACLBuilderImpl-Foreground");
            Stat resultStat = RetryLoop.callWithRetry
                (
                    client.getZookeeperClient(),
                    CallableUtils.FromFunc(() =>
                    {
                        Task<Stat> task = client.getZooKeeper().setACLAsync(path, acling.getAclList(path), version);
                        task.Wait();
                        return task.Result;
                    })
                );
            trace.commit();
            return resultStat;
        }
    }
}