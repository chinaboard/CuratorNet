using org.apache.zookeeper.data;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IStatable<T>
    {
        /**
         * Have the operation fill the provided stat object
         *
         * @param stat the stat to have filled in
         * @return this
         */
        T storingStatIn(Stat stat);
    }

}
