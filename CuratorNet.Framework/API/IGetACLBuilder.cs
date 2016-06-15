using System.Collections.Generic;
using org.apache.zookeeper.data;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IGetACLBuilder :
        IBackgroundPathable<List<ACL>>,
        IStatable<IPathable<List<ACL>>>
    {
    }
}
