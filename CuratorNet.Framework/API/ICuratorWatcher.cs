using org.apache.zookeeper;

namespace Org.Apache.CuratorNet.Framework.API
{
    /**
     * A version of {@link Watcher} that can throw an exception
     */
    public interface CuratorWatcher
    {
        /**
         * Same as {@link Watcher#process(WatchedEvent)}. If an exception
         * is thrown, Curator will log it
         *
         * @param event the event
         * @throws Exception any exceptions to log
         */
        void process(WatchedEvent @event);
    }
}
