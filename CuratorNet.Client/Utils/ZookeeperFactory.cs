using System;
using org.apache.zookeeper;

namespace Org.Apache.CuratorNet.Client.Utils
{
    public interface IZookeeperFactory
    {
        /// <summary>
        /// Allocate a new ZooKeeper instance
        /// </summary>
        /// <param name="connectString">the connection string</param>
        /// <param name="sessionTimeout">session timeout in milliseconds</param>
        /// <param name="watcher">optional watcher</param>
        /// <param name="canBeReadOnly">if true, allow ZooKeeper client to enter read only mode in case of a network partition.</param>
        /// <returns></returns>
        ZooKeeper newZooKeeper(String connectString, int sessionTimeout, Watcher watcher, bool canBeReadOnly);
    }

}
