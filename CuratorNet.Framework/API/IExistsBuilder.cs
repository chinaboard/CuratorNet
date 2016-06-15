namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IExistsBuilder : IExistsBuilderMain
    {
        /**
         * Causes any parent nodes to get created using {@link CreateMode#CONTAINER} if they haven't already been.
         * IMPORTANT NOTE: container creation is a new feature in recent versions of ZooKeeper.
         * If the ZooKeeper version you're using does not support containers, the parent nodes
         * are created as ordinary PERSISTENT nodes.
         *
         * @return this
         */
        IExistsBuilderMain creatingParentContainersIfNeeded();
    }
}
