namespace Org.Apache.CuratorNet.Framework.Imps
{
    /**
     * @see CuratorFramework#getState()
     */
    public enum CuratorFrameworkState
    {
        /**
         * {@link CuratorFramework#start()} has not yet been called
         */
        LATENT,

        /**
         * {@link CuratorFramework#start()} has been called
         */
        STARTED,

        /**
         * {@link CuratorFramework#close()} has been called
         */
        STOPPED
    }
}