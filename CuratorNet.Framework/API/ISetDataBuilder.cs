using org.apache.zookeeper.data;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface ISetDataBuilder :
        IBackgroundPathAndBytesable<Stat>,
        IVersionable<IBackgroundPathAndBytesable<Stat>>,
        Compressible<ISetDataBackgroundVersionable>
    {
    }
}
