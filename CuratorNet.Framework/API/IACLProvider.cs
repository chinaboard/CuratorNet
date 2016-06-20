using System;
using System.Collections.Generic;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Client.Utils;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IACLProvider : IInternalACLProvider
    {
        /**
         * Return the ACL list to use by default (usually {@link ZooDefs.Ids#OPEN_ACL_UNSAFE}).
         *
         * @return default ACL list
         */
        List<ACL> getDefaultAcl();

        /**
         * Return the ACL list to use for the given path
         *
         * @param path path (NOTE: might be null)
         * @return ACL list
         */
        List<ACL> getAclForPath(String path);
    }
}
