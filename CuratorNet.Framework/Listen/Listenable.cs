using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.CuratorNet.Framework.Listen
{
    /**
     * Abstracts a listenable object
     */
    public interface Listenable<T>
    {
        /**
         * Add the given listener. The listener will be executed in the containing
         * instance's thread.
         *
         * @param listener listener to add
         */
        void addListener(T listener);

        /**
         * Add the given listener. The listener will be executed using the given
         * executor
         *
         * @param listener listener to add
         * @param executor executor to run listener in
         */
        void addListener(T listener, IExecutor executor);

        /**
         * Remove the given listener
         *
         * @param listener listener to remove
         */
        void removeListener(T listener);
    }
}
