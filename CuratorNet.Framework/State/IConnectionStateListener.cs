namespace Org.Apache.CuratorNet.Framework.State
{
    public interface IConnectionStateListener
    {
        /**
         * Called when there is a state change in the connection
         *
         * @param client the client
         * @param newState the new state
         */
        void stateChanged(CuratorFramework client, ConnectionState newState);
    }
}