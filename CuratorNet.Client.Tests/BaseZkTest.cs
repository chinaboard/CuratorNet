using CuratorNet.Test;
using org.apache.zookeeper;

namespace CuratorNet.Client.Tests
{
    public class BaseZkTest
    {
        public const string ZkDefaultHosts = "10.5.20.30:2181";
        public const int DefaultSessionTimeout = 5000;
        public const int DefaultConnectionTimeout = 5000;
        public static Watcher DefaultWatcher => new NopWatcher();

        protected ZooKeeper Zookeeper;

        protected string ZkConnectionString { get; set; }

        protected BaseZkTest(string zkConnectionString,
                                int sessionTimeout,
                                Watcher watcher,
                                bool readOnly)
        {
            ZkConnectionString = zkConnectionString;
            Zookeeper = new ZooKeeper(zkConnectionString, sessionTimeout, watcher, readOnly);
        }
    }
}
