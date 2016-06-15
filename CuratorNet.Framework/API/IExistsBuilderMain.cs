using org.apache.zookeeper.data;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IExistsBuilderMain :
        IWatchable<IBackgroundPathable<Stat>>,
        IBackgroundPathable<Stat>
    {
    }

}
