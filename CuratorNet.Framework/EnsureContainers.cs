using Org.Apache.Java.Types.Concurrent.Atomics;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    /**
     * Similar to {@link org.apache.curator.utils.EnsurePath} but creates containers.
     *
     */
    public class EnsureContainers
    {
        private readonly CuratorFramework client;
        private readonly string path;
        private readonly AtomicBoolean ensureNeeded = new AtomicBoolean(true);
        private readonly object _ensureLock = new object();

        /**
         * @param client the client
         * @param path path to ensure is containers
         */
        public EnsureContainers(CuratorFramework client, string path)
        {
            this.client = client;
            this.path = path;
        }

        /**
         * The first time this method is called, all nodes in the
         * path will be created as containers if needed
         *
         * @throws Exception errors
         */
        public void ensure()
        {
            if ( ensureNeeded.get() )
            {
                internalEnsure();
            }
        }

        private void internalEnsure()
        {
            lock (_ensureLock)
            {
                if ( ensureNeeded.compareAndSet(true, false) )
                {
                    client.createContainers(path);
                }
            }
        }
    }
}