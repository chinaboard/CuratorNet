namespace Org.Apache.CuratorNet.Framework.API
{
    /**
     * Functor for an async background operation
     */
    public interface BackgroundCallback
    {
        /**
         * Called when the async background operation completes
         *
         * @param client the client
         * @param event operation result details
         * @throws Exception errors
         */
        void processResult(CuratorFramework client, CuratorEvent event);
    }
}
