using System;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface ProtectACLCreateModePathAndBytesable<T> :
        IACLBackgroundPathAndBytesable<T>,
        ICreateModable<IACLBackgroundPathAndBytesable<T>>
    {
        /**
         * <p>
         *     Hat-tip to https://github.com/sbridges for pointing this out
         * </p>
         *
         * <p>
         *     It turns out there is an edge case that exists when creating sequential-ephemeral
         *     nodes. The creation can succeed on the server, but the server can crash before
         *     the created node name is returned to the client. However, the ZK session is still
         *     valid so the ephemeral node is not deleted. Thus, there is no way for the client to
         *     determine what node was created for them.
         * </p>
         *
         * <p>
         *     Even without sequential-ephemeral, however, the create can succeed on the sever
         *     but the client (for various reasons) will not know it.
         * </p>
         *
         * <p>
         *     Putting the create builder into protection mode works around this.
         *     The name of the node that is created is prefixed with a GUID. If node creation fails
         *     the normal retry mechanism will occur. On the retry, the parent path is first searched
         *     for a node that has the GUID in it. If that node is found, it is assumed to be the lost
         *     node that was successfully created on the first try and is returned to the caller.
         * </p>
         *
         * @return this
         */
        IACLCreateModeBackgroundPathAndBytesable<String> withProtection();
    }
}
