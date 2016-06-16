using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Framework.API;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    class BackgroundSyncImpl : BackgroundOperation<String>
    {
        private readonly CuratorFrameworkImpl client;
        private readonly Object context;

        BackgroundSyncImpl(CuratorFrameworkImpl client, Object context)
        {
            this.client = client;
            this.context = context;
        }

        public void performBackgroundOperation(OperationAndData<String> operationAndData)
        {
            TimeTrace trace = client.getZookeeperClient().startTracer("BackgroundSyncImpl");
            client.getZooKeeper().sync
            (
                operationAndData.getData(),
                new AsyncCallback.VoidCallback()
                {
                    @Override
                    public void processResult(int rc, String path, Object ctx)
                    {
                        trace.commit();
                        CuratorEventImpl event = new CuratorEventImpl(client, CuratorEventType.SYNC, rc, path, null, ctx, null, null, null, null, null);
                        client.processBackgroundOperation(operationAndData, event);
                    }
                },
                context
            );
        }
    }
}
