using org.apache.zookeeper;

namespace Org.Apache.CuratorNet.Client.Utils
{
    public class DefaultZookeeperFactory : IZookeeperFactory
    {
        public ZooKeeper newZooKeeper(string connectString, int sessionTimeout, 
                                        Watcher watcher, bool canBeReadOnly)
        {
            return new ZooKeeper(connectString, sessionTimeout, watcher, canBeReadOnly);
        }
    }
}
