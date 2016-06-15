using org.apache.zookeeper;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IWatchable<T>
    {
        /**
         * Have the operation set a watch
         *
         * @return this
         */
        T watched();

        /**
         * Set a watcher for the operation
         *
         * @param watcher the watcher
         * @return this
         */
        T usingWatcher(Watcher watcher);

        /**
         * Set a watcher for the operation
         *
         * @param watcher the watcher
         * @return this
         */
        T usingWatcher(CuratorWatcher watcher);
    }
}