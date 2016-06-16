using System;
using System.Collections.Generic;
using NLog;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.Java.Types.Concurrent.Atomics;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class FindAndDeleteProtectedNodeInBackground : IBackgroundOperation<Object>
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly CuratorFrameworkImpl client;
        private readonly String namespaceAdjustedParentPath;
        private readonly String protectedId;

        FindAndDeleteProtectedNodeInBackground(CuratorFrameworkImpl client, 
                                                String namespaceAdjustedParentPath, 
                                                String protectedId)
        {
            this.client = client;
            this.namespaceAdjustedParentPath = namespaceAdjustedParentPath;
            this.protectedId = protectedId;
        }

        internal void execute()
        {
            var errorCallback = new OperationAndData.ErrorCallback<object>()
            {
                @Override
                public void retriesExhausted(OperationAndData<Void> operationAndData)
                {
                    operationAndData.reset();
                    client.processBackgroundOperation(operationAndData, null);
                }
            };
            var operationAndData = new OperationAndData<Void>(this, null, null, errorCallback, null);
            client.processBackgroundOperation(operationAndData, null);
        }

        static readonly AtomicBoolean debugInsertError = new AtomicBoolean(false);

        public void performBackgroundOperation(OperationAndData<object> operationAndData)
        {
            TimeTrace trace = client.getZookeeperClient().startTracer("FindAndDeleteProtectedNodeInBackground");
            var callback = new AsyncCallback.Children2Callback()
            {
                @Override
                public void processResult(int rc, String path, Object o, List<String> strings, Stat stat)
                {
                    trace.commit();

                    if (debugInsertError.compareAndSet(true, false))
                    {
                        rc = KeeperException.Code.CONNECTIONLOSS.intValue();
                    }

                    if (rc == KeeperException.Code.OK.intValue())
                    {
                        String node = CreateBuilderImpl.findNode(strings, "/", protectedId);  // due to namespacing, don't let CreateBuilderImpl.findNode adjust the path
                        if (node != null)
                        {
                            try
                            {
                                String deletePath = client.unfixForNamespace(ZKPaths.makePath(namespaceAdjustedParentPath, node));
                                client.delete().guaranteed().inBackground().forPath(deletePath);
                            }
                            catch (Exception e)
                            {
                                ThreadUtils.checkInterrupted(e);
                                log.Error("Could not start guaranteed delete for node: " + node);
                                rc = KeeperException.Code.CONNECTIONLOSS.intValue();
                            }
                        }
                    }

                    if (rc != KeeperException.Code.OK.intValue())
                    {
                        CuratorEventImpl event = new CuratorEventImpl(client, CuratorEventType.CHILDREN, rc, path, null, o, stat, null, strings, null, null);
                        client.processBackgroundOperation(operationAndData, event);
                    }
                }
            };
            client.getZooKeeper().getChildren(namespaceAdjustedParentPath, false, callback, null);
        }
    }
}