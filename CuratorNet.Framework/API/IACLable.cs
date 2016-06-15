using System.Collections.Generic;
using org.apache.zookeeper.data;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IACLable<T>
    {
        /**
         * Set an ACL list (default is {@link ZooDefs.Ids#OPEN_ACL_UNSAFE})
         *
         * @param aclList the ACL list to use
         * @return this
         */
        T withACL(List<ACL> aclList);
    }
}
