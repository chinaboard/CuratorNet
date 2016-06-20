using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.CuratorNet.Framework
{
    /**
     * Generic holder POJO for a listener and its executor
     * @param <T> the listener type
     */
    public class ListenerEntry<T>
    {
        public readonly T        listener;
        public readonly IExecutor executor;

        public ListenerEntry(T listener, IExecutor executor)
        {
            this.listener = listener;
            this.executor = executor;
        }
    }
}