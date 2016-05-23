using System;
using System.Collections.Generic;
using org.apache.zookeeper.data;

namespace Org.Apache.CuratorNet.Client.Utils
{
    public interface IInternalACLProvider
    {
        /**
         * Return the ACL list to use by default (usually {@link ZooDefs.Ids#OPEN_ACL_UNSAFE}).
         *
         * @return default ACL list
         */
        IList<ACL> getDefaultAcl();

        /**
         * Return the ACL list to use for the given path
         *
         * @param path path (NOTE: might be null)
         * @return ACL list
         */
        IList<ACL> getAclForPath(String path);
    }

}
