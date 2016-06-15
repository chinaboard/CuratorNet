using System;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface PathAndBytesable<T>
    {
        /**
         * Commit the currently building operation using the given path and data
         *
         * @param path the path
         * @param data the data
         * @return operation result if any
         * @throws Exception errors
         */
        T forPath(String path, byte[] data);

        /**
         * Commit the currently building operation using the given path and the default data
         * for the client (usually a byte[0] unless changed via
         * {@link CuratorFrameworkFactory.Builder#defaultData(byte[])}).
         *
         * @param path the path
         * @return operation result if any
         * @throws Exception errors
         */
        T forPath(String path);
    }

}
