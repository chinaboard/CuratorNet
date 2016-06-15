namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IGuaranteeable : IBackgroundVersionable
    {
        /**
         * <p>
         *     Solves this edge case: deleting a node can fail due to connection issues. Further,
         *     if the node was ephemeral, the node will not get auto-deleted as the session is still valid.
         *     This can wreak havoc with lock implementations.
         * </p>
         *
         * <p>
         *     When <code>guaranteed</code> is set, Curator will record failed node deletions and
         *     attempt to delete them in the background until successful. NOTE: you will still get an
         *     exception when the deletion fails. But, you can be assured that as long as the
         *     {@link org.apache.curator.framework.CuratorFramework} instance is open attempts will be made to delete the node.
         * </p>
         *  
         * @return this
         */
        IChildrenDeletable guaranteed();
    }
}
