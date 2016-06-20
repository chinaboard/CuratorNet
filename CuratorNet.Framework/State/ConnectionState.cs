namespace Org.Apache.CuratorNet.Framework.State
{
    /**
     * Represents state changes in the connection to ZK
     */
    public enum ConnectionState
    {
        /// <summary>
        /// Sent for the first successful connection to the server. NOTE: You will only
        /// get one of these messages for any CuratorFramework instance.
        /// </summary>
        CONNECTED,

        /// <summary>
        /// There has been a loss of connection. Leaders, locks, etc. should suspend
        /// until the connection is re-established. If the connection times-out you will
        /// receive a {@link #LOST} notice
        /// </summary>
        SUSPENDED,

        /// <summary>
        /// A suspended, lost, or read-only connection has been re-established
        /// </summary>
        RECONNECTED,

        /// <summary>
        /// The connection is confirmed to be lost. Close any locks, leaders, etc. and
        /// attempt to re-create them. NOTE: it is possible to get a {@link #RECONNECTED}
        /// state after this but you should still consider any locks, etc. as dirty/unstable
        /// </summary>
        LOST,

        /// <summary>
        /// The connection has gone into read-only mode. This can only happen if you pass true 
        /// for {@link CuratorFrameworkFactory.Builder#canBeReadOnly()}. See the ZooKeeper doc
        /// regarding read only connections: 
        /// <a href = "http://wiki.apache.org/hadoop/ZooKeeper/GSoCReadOnlyMode" > http://wiki.apache.org/hadoop/ZooKeeper/GSoCReadOnlyMode</a>.
        /// The connection will remain in read only mode until another state change is sent.
        /// </summary>
        READ_ONLY
    }

    public static class ConnectionStateUtils
    {
        /// <summary>
        /// Check if this state indicates that Curator has a connection to ZooKeeper
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool IsConnected(ConnectionState state)
        {
            return state == ConnectionState.CONNECTED 
                    || state == ConnectionState.RECONNECTED 
                    || state == ConnectionState.READ_ONLY;
        }
    }
}
