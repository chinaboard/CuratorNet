using System;
using System.Collections.Generic;
using org.apache.zookeeper.data;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class ACLing
    {
        private readonly List<ACL>     aclList;
        private readonly ACLProvider   aclProvider;

        ACLing(ACLProvider aclProvider)
        {
            this(aclProvider, null);
        }

        ACLing(ACLProvider aclProvider, List<ACL> aclList)
        {
            this.aclProvider = aclProvider;
            this.aclList = (aclList != null) ? ImmutableList.copyOf(aclList) : null;
        }

        List<ACL> getAclList(String path)
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
