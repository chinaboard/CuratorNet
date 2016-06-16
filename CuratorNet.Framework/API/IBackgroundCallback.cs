namespace Org.Apache.CuratorNet.Framework.API
{
    /**
     * Functor for an async background operation
     */
    public interface IBackgroundCallback
    {
        /**
         * Called when the async background operation completes
         *
         * @param client the client
         * @param event operation result details
         * @throws Exception errors
         */
        void processResult(CuratorFramework client, ICuratorEvent @event);
    }
}
