namespace Org.Apache.CuratorNet.Framework.API
{
    /**
     * Receives notifications about errors and background events
     */
    public interface ICuratorListener
    {
        /**
         * Called when a background task has completed or a watch has triggered
         *
         * @param client client
         * @param event the event
         * @throws Exception any errors
         */
        void eventReceived(CuratorFramework client, ICuratorEvent @event);
    }
}
