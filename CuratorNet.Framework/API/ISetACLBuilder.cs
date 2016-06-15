using org.apache.zookeeper.data;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface ISetACLBuilder :
        IACLable<IBackgroundPathable<Stat>>,
        IVersionable<IACLable<IBackgroundPathable<Stat>>>
    {
    }

}
