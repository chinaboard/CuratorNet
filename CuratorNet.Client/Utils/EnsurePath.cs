using System;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Atomics;

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

        public interface Helper
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
        public EnsurePath(String path) : this(path, null, true, null)
        {
            
        }

        /**
         * @param path the full path to ensure
         * @param aclProvider if not null, the ACL provider to use when creating parent nodes
         */
        public EnsurePath(String path, IInternalACLProvider aclProvider) 
            : this(path, null, true, aclProvider)
        {
            
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
            Helper localHelper = helper.Get();
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
            this.helper = helper ?? new AtomicReference<Helper>(new InitialHelper(this));
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
            private readonly EnsurePath _ensurePath;
            private bool isSet = false;  // guarded by synchronization

            /// <summary>
            /// Initializes a new instance of the <see cref="T:System.Object"/> class.
            /// </summary>
            public InitialHelper(EnsurePath ensurePath)
            {
                _ensurePath = ensurePath;
            }

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
                             CallableUtils.FromFunc<object>(() =>
                             {
                                 ZKPaths.mkdirs(client.getZooKeeper(), 
                                                    path, 
                                                    makeLastNode,
                                                    _ensurePath.aclProvider,
                                                    _ensurePath.asContainers());
                                 _ensurePath.helper.Set(doNothingHelper);
                                 isSet = true;
                                 return null;
                             })
                        );
                    }
                }
            }
        }
    }
}
