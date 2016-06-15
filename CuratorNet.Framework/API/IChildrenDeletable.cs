namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IChildrenDeletable : IBackgroundVersionable
    {

        /**
         * <p>
         *     Will also delete children if they exist.
         * </p>
         * @return
         */
        IBackgroundVersionable deletingChildrenIfNeeded();
    }
}
