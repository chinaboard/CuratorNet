using org.apache.zookeeper;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface ICreateModable<T>
    {
        /**
         * Set a create mode - the default is {@link CreateMode#PERSISTENT}
         *
         * @param mode new create mode
         * @return this
         */
        T withMode(CreateMode mode);
    }
}
