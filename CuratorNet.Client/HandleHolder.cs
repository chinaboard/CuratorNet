using System;
using System.Threading;
using System.Threading.Tasks;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Client.Utils;

namespace Org.Apache.CuratorNet.Client
{
    internal class HandleHolder
    {
        private readonly IZookeeperFactory zookeeperFactory;
        private readonly Watcher watcher;
        private readonly int sessionTimeout;
        private readonly bool canBeReadOnly;

        private volatile Helper helper;

        private interface Helper
        {
            ZooKeeper getZooKeeper();

            String getConnectionString();
        }

        HandleHolder(IZookeeperFactory zookeeperFactory, 
                        Watcher watcher, 
                        int sessionTimeout, 
                        bool canBeReadOnly)
        {
            this.zookeeperFactory = zookeeperFactory;
            this.watcher = watcher;
            this.sessionTimeout = sessionTimeout;
            this.canBeReadOnly = canBeReadOnly;
        }

        ZooKeeper getZooKeeper()
        {
            return (helper != null) ? helper.getZooKeeper() : null;
        }

        String getConnectionString()
        {
            return (helper != null) ? helper.getConnectionString() : null;
        }

        bool hasNewConnectionString()
        {
            String helperConnectionString = (helper != null) ? helper.getConnectionString() : null;
            return (helperConnectionString != null) /*&& !ensembleProvider.getConnectionString().equals(helperConnectionString)*/;
        }

        void closeAndClear()
        {
            internalClose();
            helper = null;
        }

        void closeAndReset()
        {
            internalClose();

            // first helper is synchronized when getZooKeeper is called. Subsequent calls
            // are not synchronized.
            helper = new CloseHelper();
        }

        private async void internalClose()
        {
            try
            {
                ZooKeeper zooKeeper = helper?.getZooKeeper();
                if (zooKeeper != null)
                {
                    Watcher dummyWatcher = new EmptyWatcher();
//                    zooKeeper.register(dummyWatcher);// clear the default watcher so that no new events get processed by mistake
                    await zooKeeper.closeAsync();
                }
            }
            catch ( Exception dummy )
            {
                Thread.CurrentThread.Abort();
            }
        }

        internal class EmptyWatcher : Watcher
        {
            public override Task process(WatchedEvent @event)
            {
                return Task.FromResult<object>(null);
            }
        }

        internal class CloseHelper : Helper
        {
            private volatile ZooKeeper zooKeeperHandle = null;
            private volatile String connectionString = null;

            public ZooKeeper getZooKeeper()
            {
                lock (this)
                {
                    if (zooKeeperHandle == null)
                    {
                        connectionString = ensembleProvider.getConnectionString();
                        zooKeeperHandle = zookeeperFactory.newZooKeeper(connectionString, 
                                                                        sessionTimeout, 
                                                                        watcher, 
                                                                        canBeReadOnly);
                    }

                    helper = new Helper()
                    {
                        public ZooKeeper getZooKeeper()
                        {
                            return zooKeeperHandle;
                        }

                        public String getConnectionString()
                        {
                            return connectionString;
                        }
                    };
                    return zooKeeperHandle;
                }
            }

            public String getConnectionString()
            {
                return connectionString;
            }
        }
    }
}
