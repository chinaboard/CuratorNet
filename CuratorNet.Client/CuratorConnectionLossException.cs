using System;
using org.apache.zookeeper;

namespace Org.Apache.CuratorNet.Client
{
    /**
     * This is needed to differentiate between ConnectionLossException thrown by ZooKeeper
     * and ConnectionLossException thrown by {@link ConnectionState#checkTimeouts()}
     */
    public class CuratorConnectionLossException : ApplicationException
    {
        public CuratorConnectionLossException(){ }

        public CuratorConnectionLossException(KeeperException.ConnectionLossException connectionLossException)
            : base("",connectionLossException) {}
    }
}
