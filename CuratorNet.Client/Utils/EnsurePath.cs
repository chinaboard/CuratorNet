using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Apache.CuratorNet.Client.Utils
{
    /**
     * <p>
     * Utility to ensure that a particular path is created.
     * </p>
     * <p>
     * The first time it is used, a synchronized call to {@link ZKPaths#mkdirs(ZooKeeper, String)} is made to
     * ensure that the entire path has been created (with an empty byte array if needed). Subsequent
     * calls with the instance are un-synchronized NOPs.
     * </p>
     * <p>
     * Usage:<br>
     * </p>
     * <pre>
     *         EnsurePath       ensurePath = new EnsurePath(aFullPathToEnsure);
     *         ...
     *         String           nodePath = aFullPathToEnsure + "/foo";
     *         ensurePath.ensure(zk);   // first time syncs and creates if needed
     *         zk.create(nodePath, ...);
     *         ...
     *         ensurePath.ensure(zk);   // subsequent times are NOPs
     *         zk.create(nodePath, ...);
     * </pre>
     *
     * @deprecated Since 2.9.0 - Prefer CuratorFramework.create().creatingParentContainersIfNeeded() or CuratorFramework.exists().creatingParentContainersIfNeeded()
     */
    [Obsolete]
    public class EnsurePath
    {
        private readonly String path;
        private readonly bool makeLastNode;
        private readonly IInternalACLProvider aclProvider;
        private readonly AtomicReference<Helper> helper;

        private static readonly Helper doNothingHelper = new NoopHelper();

        interface Helper
        {
            void ensure(CuratorZookeeperClient client, string path, bool makeLastNode);
        }

        class NoopHelper : Helper
        {
            public void ensure(CuratorZookeeperClient client, string path, bool makeLastNode)
            {
            }
        }
        
        /**
         * @param path the full path to ensure
         */
        public EnsurePath(String path)
        {
            this(path, null, true, null);
        }

        /**
         * @param path the full path to ensure
         * @param aclProvider if not null, the ACL provider to use when creating parent nodes
         */
        public EnsurePath(String path, IInternalACLProvider aclProvider)
        {
            this(path, null, true, aclProvider);
        }

        /**
         * First time, synchronizes and makes sure all nodes in the path are created. Subsequent calls
         * with this instance are NOPs.
         *
         * @param client ZK client
         * @throws Exception ZK errors
         */
        public void ensure(CuratorZookeeperClient client)
        {
            Helper localHelper = helper.get();
            localHelper.ensure(client, path, makeLastNode);
        }

        /**
         * Returns a view of this EnsurePath instance that does not make the last node.
         * i.e. if the path is "/a/b/c" only "/a/b" will be ensured
         *
         * @return view
         */
        public EnsurePath excludingLast()
        {
            return new EnsurePath(path, helper, false, aclProvider);
        }

        protected EnsurePath(String path, 
                                AtomicReference<Helper> helper, 
                                bool makeLastNode, 
                                IInternalACLProvider aclProvider)
        {
            this.path = path;
            this.makeLastNode = makeLastNode;
            this.aclProvider = aclProvider;
            this.helper = (helper != null) 
                                ? helper 
                                : new AtomicReference<Helper>(new InitialHelper());
        }

        /**
         * Returns the path being Ensured
         *
         * @return the path being ensured
         */
        public String getPath()
        {
            return this.path;
        }

        protected bool asContainers()
        {
            return false;
        }

        private class InitialHelper : Helper
        {
            private bool isSet = false;  // guarded by synchronization

            public void ensure(CuratorZookeeperClient client, 
                                                string path, 
                                                bool makeLastNode)
            {
                lock (this)
                {
                    if ( !isSet )
                    {
                        RetryLoop.callWithRetry
                            (
                             client,
                             new Callable<Object>()
                             {
                                 @Override
                             public Object call() throws Exception
                                 {
                                 ZKPaths.mkdirs(client.getZooKeeper(), path, makeLastNode, aclProvider, asContainers());
                                 helper.set(doNothingHelper);
                                 isSet = true;
                                 return null;
                             }
                    }
                    );
                    }
                }
            }
        }
    }
}
