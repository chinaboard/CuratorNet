using System;
using System.Collections.Generic;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Framework.API;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    public class DefaultACLProvider : IACLProvider
    {
        public List<ACL> getDefaultAcl()
        {
            return ZooDefs.Ids.OPEN_ACL_UNSAFE;
        }

        public List<ACL> getAclForPath(String path)
        {
            return ZooDefs.Ids.OPEN_ACL_UNSAFE;
        }
    }
}