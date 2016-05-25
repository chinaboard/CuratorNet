using System;
using System.Threading;
using System.Threading.Tasks;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Client.Ensemble;
using Org.Apache.CuratorNet.Client.Utils;

namespace Org.Apache.CuratorNet.Client
{
    internal class HandleHolder
    {
        private readonly IZookeeperFactory zookeeperFactory;
        private readonly Watcher watcher;
        private readonly IEnsembleProvider ensembleProvider;
        private readonly int sessionTimeout;
        private readonly bool canBeReadOnly;

        private volatile Helper helper;

        private interface Helper
        {
            ZooKeeper getZooKeeper();

            String getConnectionString();
        }

        internal HandleHolder(IZookeeperFactory zookeeperFactory, 
                        Watcher watcher, 
                        IEnsembleProvider ensembleProvider, 
                        int sessionTimeout, 
                        bool canBeReadOnly)
        {
            this.zookeeperFactory = zookeeperFactory;
            this.watcher = watcher;
            this.ensembleProvider = ensembleProvider;
            this.sessionTimeout = sessionTimeout;
            this.canBeReadOnly = canBeReadOnly;
        }

        internal ZooKeeper getZooKeeper()
        {
            return (helper != null) ? helper.getZooKeeper() : null;
        }

        internal String getConnectionString()
        {
            return (helper != null) ? helper.getConnectionString() : null;
        }

        internal bool hasNewConnectionString()
        {
            String helperConnectionString = (helper != null) ? helper.getConnectionString() : null;
            return (helperConnectionString != null) 
                        && !ensembleProvider.getConnectionString().Equals(helperConnectionString);
        }

        internal void closeAndClear()
        {
            internalClose();
            helper = null;
        }

        private void internalClose()
        {
            try
            {
                ZooKeeper zooKeeper = (helper != null) ? helper.getZooKeeper() : null;
                if (zooKeeper != null)
                {
//                    Watcher dummyWatcher = new EmptyWatcher();
//                    zooKeeper.register(dummyWatcher);   // clear the default watcher so that no new events get processed by mistake
                    zooKeeper.closeAsync();
                }
            }
            catch ( Exception )
            {
                Thread.CurrentThread.Abort();
            }
        }

        class EmptyWatcher : Watcher
        {
            public override Task process(WatchedEvent @event)
            {
                return Task.FromResult<object>(null);
            }
        }

        class CloseHelper : Helper
        {
            private readonly HandleHolder _handleHolder;
            private volatile ZooKeeper zooKeeperHandle;
            private volatile String connectionString;

            /// <summary>
            /// Initializes a new instance of the <see cref="T:System.Object"/> class.
            /// </summary>
            public CloseHelper(HandleHolder handleHolder)
            {
                _handleHolder = handleHolder;
            }

            public ZooKeeper getZooKeeper()
            {
                lock(this)
                {
                    if (zooKeeperHandle == null)
                    {
                        connectionString = _handleHolder.ensembleProvider
                                                        .getConnectionString();
                        zooKeeperHandle = _handleHolder.zookeeperFactory
                                                       .newZooKeeper(connectionString,
                                                                        _handleHolder.sessionTimeout,
                                                                        _handleHolder.watcher,
                                                                        _handleHolder.canBeReadOnly);
                    }

                    _handleHolder.helper = new UnsyncHelper(this);

                    return zooKeeperHandle;
                }
            }

            public String getConnectionString()
            {
                return connectionString;
            }

            class UnsyncHelper : Helper
            {
                private readonly CloseHelper _helper;

                /// <summary>
                /// Initializes a new instance of the <see cref="T:System.Object"/> class.
                /// </summary>
                public UnsyncHelper(CloseHelper helper)
                {
                    _helper = helper;
                }

                public ZooKeeper getZooKeeper()
                {
                    return _helper.zooKeeperHandle;
                }

                public string getConnectionString()
                {
                    return _helper.connectionString;
                }
            }
        }

        internal void closeAndReset()
        {
            internalClose();

            // first helper is synchronized when getZooKeeper is called. Subsequent calls
            // are not synchronized.
            helper = new CloseHelper(this);
        }
    }
}
