using System;
using System.Collections.Generic;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Framework.API;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class ACLing
    {
        private readonly List<ACL>     aclList;
        private readonly IACLProvider   aclProvider;

        internal ACLing(IACLProvider aclProvider)
        {
            this.aclProvider = aclProvider;
        }

        internal ACLing(IACLProvider aclProvider, List<ACL> aclList)
        {
            this.aclProvider = aclProvider;
            this.aclList = (aclList != null) ? new List<ACL>(aclList) : null;
        }

        internal List<ACL> getAclList(String path)
        {
            List<ACL> localAclList = aclList;
            do
            {
                if (localAclList != null)
                {
                    break;
                }

                if (path != null)
                {
                    localAclList = aclProvider.getAclForPath(path);
                    if (localAclList != null)
                    {
                        break;
                    }
                }

                localAclList = aclProvider.getDefaultAcl();
            } while (false);
            return localAclList;
        }
    }
}
