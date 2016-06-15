using System;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IPathable<T>
    {
        /**
         * Commit the currently building operation using the given path
         *
         * @param path the path
         * @return operation result if any
         * @throws Exception errors
         */
        T forPath(String path);
    }
}
