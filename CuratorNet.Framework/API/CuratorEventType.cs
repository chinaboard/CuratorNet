namespace Org.Apache.CuratorNet.Framework.API
{
    public enum CuratorEventType
    {
        /**
         * Corresponds to {@link CuratorFramework#create()}
         */
        CREATE,

        /**
         * Corresponds to {@link CuratorFramework#delete()}
         */
        DELETE,

        /**
         * Corresponds to {@link CuratorFramework#checkExists()}
         */
        EXISTS,

        /**
         * Corresponds to {@link CuratorFramework#getData()}
         */
        GET_DATA,

        /**
         * Corresponds to {@link CuratorFramework#setData()}
         */
        SET_DATA,

        /**
         * Corresponds to {@link CuratorFramework#getChildren()}
         */
        CHILDREN,

        /**
         * Corresponds to {@link CuratorFramework#sync(String, Object)}
         */
        SYNC,

        /**
         * Corresponds to {@link CuratorFramework#getACL()}
         */
        GET_ACL,

        /**
         * Corresponds to {@link CuratorFramework#setACL()}
         */
        SET_ACL,

        /**
         * Corresponds to {@link Watchable#usingWatcher(Watcher)} or {@link Watchable#watched()}
         */
        WATCHED,

        /**
         * Event sent when client is being closed
         */
        CLOSING
    }

}
