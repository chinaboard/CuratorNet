using org.apache.zookeeper.data;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface ISetDataBackgroundVersionable :
        IBackgroundPathAndBytesable<Stat>,
        IVersionable<IBackgroundPathAndBytesable<Stat>>
    {
    }
}
