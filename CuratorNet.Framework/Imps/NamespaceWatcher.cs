using System;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.CuratorNet.Framework.API;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class NamespaceWatcher : Watcher, IDisposable
    {
        private volatile CuratorFrameworkImpl client;
        private volatile Watcher actualWatcher;
        private volatile CuratorWatcher curatorWatcher;

        NamespaceWatcher(CuratorFrameworkImpl client, Watcher actualWatcher)
        {
            this.client = client;
            this.actualWatcher = actualWatcher;
            this.curatorWatcher = null;
        }

        NamespaceWatcher(CuratorFrameworkImpl client, CuratorWatcher curatorWatcher)
        {
            this.client = client;
            this.actualWatcher = null;
            this.curatorWatcher = curatorWatcher;
        }

        public void Dispose()
        {
            client = null;
            actualWatcher = null;
            curatorWatcher = null;
        }

        public void process(WatchedEvent @event)
        {
            if (client != null)
            {
                if (actualWatcher != null)
                {
                    actualWatcher.process(new NamespaceWatchedEvent(client, @event));
                }
                else if (curatorWatcher != null)
                {
                    try
                    {
                        curatorWatcher.process(new NamespaceWatchedEvent(client, @event));
                    }
                    catch (Exception e)
                    {
                        ThreadUtils.checkInterrupted(e);
                        client.logError("Watcher exception", e);
                    }
                }
            }
        }
    }
}