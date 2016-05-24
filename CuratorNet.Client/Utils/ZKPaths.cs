using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using org.apache.zookeeper;
using org.apache.zookeeper.data;

namespace Org.Apache.CuratorNet.Client.Utils
{
    public class ZKPaths
    {
        /**
         * Zookeeper's path separator character.
         */
        public const String PATH_SEPARATOR = "/";

        private static readonly CreateMode NON_CONTAINER_MODE = CreateMode.PERSISTENT;

        /**
         * @return {@link CreateMode#CONTAINER} if the ZK JAR supports it. Otherwise {@link CreateMode#PERSISTENT}
         */
        public static CreateMode getContainerCreateMode()
        {
            return CreateModeHolder.containerCreateMode;
        }

        /**
         * Returns true if the version of ZooKeeper client in use supports containers
         *
         * @return true/false
         */
        public static bool hasContainerSupport()
        {
            return getContainerCreateMode() != NON_CONTAINER_MODE;
        }

        private static class CreateModeHolder
        {
            internal static readonly Logger log = LogManager.GetCurrentClassLogger();
            internal static readonly CreateMode containerCreateMode;

            static CreateModeHolder()
            {
                CreateMode localCreateMode;
                try
                {
                    //localCreateMode = CreateMode.valueOf("CONTAINER");
                    localCreateMode = NON_CONTAINER_MODE;
                }
                catch ( Exception e )
                {
                    localCreateMode = NON_CONTAINER_MODE;
                    log.Warn("The version of ZooKeeper being used doesn't support Container nodes. " +
                             "CreateMode.PERSISTENT will be used instead.");
                }
                containerCreateMode = localCreateMode;
            }
        }

        /**
         * Apply the namespace to the given path
         *
         * @param namespace namespace (can be null)
         * @param path      path
         * @return adjusted path
         */
        public static String fixForNamespace(String ns, String path)
        {
            return fixForNamespace(ns, path, false);
        }

        /**
         * Apply the namespace to the given path
         *
         * @param namespace    namespace (can be null)
         * @param path         path
         * @param isSequential if the path is being created with a sequential flag
         * @return adjusted path
         */
        public static String fixForNamespace(String ns, String path, bool isSequential)
        {
            // Child path must be valid in and of itself.
            PathUtils.validatePath(path, isSequential);

            if ( ns != null )
            {
                return makePath(ns, path);
            }
            return path;
        }

        /**
         * Given a full path, return the node name. i.e. "/one/two/three" will return "three"
         *
         * @param path the path
         * @return the node
         */
        public static String getNodeFromPath(String path)
        {
            PathUtils.validatePath(path);
            int i = path.LastIndexOf(PATH_SEPARATOR);
            if (i < 0)
            {
                return path;
            }
            if ((i + 1) >= path.Length)
            {
                return "";
            }
            return path.Substring(i + 1);
        }

        public class PathAndNode
        {
            private readonly String path;
            private readonly String node;

            public PathAndNode(String path, String node)
            {
                this.path = path;
                this.node = node;
            }

            public String getPath()
            {
                return path;
            }

            public String getNode()
            {
                return node;
            }
        }

        /**
         * Given a full path, return the node name and its path. i.e. "/one/two/three" will return {"/one/two", "three"}
         *
         * @param path the path
         * @return the node
         */
        public static PathAndNode getPathAndNode(String path)
        {
            PathUtils.validatePath(path);
            int i = path.LastIndexOf(PATH_SEPARATOR);
            if (i < 0)
            {
                return new PathAndNode(path, "");
            }
            if ((i + 1) >= path.Length)
            {
                return new PathAndNode(PATH_SEPARATOR, "");
            }
            String node = path.Substring(i + 1);
            String parentPath = (i > 0) ? path.Substring(0, i) : PATH_SEPARATOR;
            return new PathAndNode(parentPath, node);
        }

        /**
         * Given a full path, return the the individual parts, without slashes.
         * The root path will return an empty list.
         *
         * @param path the path
         * @return an array of parts
         */

        public static List<String> split(String path)
        {
            PathUtils.validatePath(path);
            return SplitPath(path);
//            return PATH_SPLITTER.splitToList(path);
        }

//        private static readonly Splitter PATH_SPLITTER 
//            = Splitter.on(PATH_SEPARATOR).omitEmptyStrings();

        private static List<string> SplitPath(string path)
        {
            return path.Split(new[] { PATH_SEPARATOR }, 
                              StringSplitOptions.RemoveEmptyEntries)
                       .ToList();
        }

        /**
         * Make sure all the nodes in the path are created. 
         * NOTE: Unlike File.mkdirs(), Zookeeper doesn't distinguish
         * between directories and files. So, every node in the path is created. 
         * The data for each node is an empty blob.
         *
         * @param zookeeper the client
         * @param path      path to ensure
         * @throws InterruptedException                 thread interruption
         * @throws org.apache.zookeeper.KeeperException Zookeeper errors
         */
        public static void mkdirs(ZooKeeper zookeeper, String path)
        {
            mkdirs(zookeeper, path, true, null, false);
        }

        /**
         * Make sure all the nodes in the path are created. 
         * NOTE: Unlike File.mkdirs(), Zookeeper doesn't distinguish
         * between directories and files. So, every node in the path is created. 
         * The data for each node is an empty blob.
         *
         * @param zookeeper    the client
         * @param path         path to ensure
         * @param makeLastNode if true, all nodes are created. If false, only the parent nodes are created
         * @throws InterruptedException                 thread interruption
         * @throws org.apache.zookeeper.KeeperException Zookeeper errors
         */
        public static void mkdirs(ZooKeeper zookeeper, 
                                    String path, 
                                    bool makeLastNode)
            {
                mkdirs(zookeeper, path, makeLastNode, null, false);
        }

        /**
         * Make sure all the nodes in the path are created. NOTE: Unlike File.mkdirs(), Zookeeper doesn't distinguish
         * between directories and files. So, every node in the path is created. The data for each node is an empty blob
         *
         * @param zookeeper    the client
         * @param path         path to ensure
         * @param makeLastNode if true, all nodes are created. If false, only the parent nodes are created
         * @param aclProvider  if not null, the ACL provider to use when creating parent nodes
         * @throws InterruptedException                 thread interruption
         * @throws org.apache.zookeeper.KeeperException Zookeeper errors
         */
        public static void mkdirs(ZooKeeper zookeeper, 
                                    String path, 
                                    bool makeLastNode, 
                                    IInternalACLProvider aclProvider)
            {
                mkdirs(zookeeper, path, makeLastNode, aclProvider, false);
            }

    /**
     * Make sure all the nodes in the path are created. NOTE: Unlike File.mkdirs(), Zookeeper doesn't distinguish
     * between directories and files. So, every node in the path is created. The data for each node is an empty blob
     *
     * @param zookeeper    the client
     * @param path         path to ensure
     * @param makeLastNode if true, all nodes are created. If false, only the parent nodes are created
     * @param aclProvider  if not null, the ACL provider to use when creating parent nodes
     * @param asContainers if true, nodes are created as {@link CreateMode#CONTAINER}
     * @throws InterruptedException                 thread interruption
     * @throws org.apache.zookeeper.KeeperException Zookeeper errors
     */
    public static async void mkdirs(ZooKeeper zookeeper, 
                                String path, 
                                bool makeLastNode, 
                                IInternalACLProvider aclProvider, 
                                bool asContainers)
    {
        PathUtils.validatePath(path);

        int pos = 1; // skip first slash, root is guaranteed to exist
        do
        {
            pos = path.IndexOf(PATH_SEPARATOR, pos + 1);

            if ( pos == -1 )
            {
                if ( makeLastNode )
                {
                    pos = path.Length;
                }
                else
                {
                    break;
                }
            }

            String subPath = path.Substring(0, pos);
            if ( await zookeeper.existsAsync(subPath, false) == null )
            {
                try
                {
                    IList<ACL> acl = null;
                    if ( aclProvider != null )
                    {
                        acl = aclProvider.getAclForPath(subPath);
                        if ( acl == null )
                        {
                            acl = aclProvider.getDefaultAcl();
                        }
                    }
                    if ( acl == null )
                    {
                        acl = ZooDefs.Ids.OPEN_ACL_UNSAFE;
                    }
                    zookeeper.createAsync(subPath, 
                                            new byte[0], 
                                            acl.ToList(), 
                                            getCreateMode(asContainers));
                }
                catch ( KeeperException.NodeExistsException e )
                {
                    // ignore... someone else has created it since we checked
                }
            }

        }
        while ( pos<path.Length );
    }

    /**
     * Recursively deletes children of a node.
     *
     * @param zookeeper  the client
     * @param path       path of the node to delete
     * @param deleteSelf flag that indicates that the node should also get deleted
     * @throws InterruptedException
     * @throws KeeperException
     */
    public static async void deleteChildren(ZooKeeper zookeeper, 
                                        String path, 
                                        bool deleteSelf)
    {
        PathUtils.validatePath(path);

        ChildrenResult children = await zookeeper.getChildrenAsync(path, null);
        foreach( String child in children.Children )
        {
            String fullPath = makePath(path, child);
            deleteChildren(zookeeper, fullPath, true);
        }

        if ( deleteSelf )
        {
            try
            {
                await zookeeper.deleteAsync(path, -1);
            }
            catch ( KeeperException.NotEmptyException e )
            {
                //someone has created a new child since we checked ... delete again.
                deleteChildren(zookeeper, path, true);
            }
            catch ( KeeperException.NoNodeException e )
            {
                // ignore... someone else has deleted the node it since we checked
            }
        }
    }

    /**
     * Return the children of the given path sorted by sequence number
     *
     * @param zookeeper the client
     * @param path      the path
     * @return sorted list of children
     * @throws InterruptedException                 thread interruption
     * @throws org.apache.zookeeper.KeeperException zookeeper errors
     */
    public static async Task<List<string>> getSortedChildren(ZooKeeper zookeeper, String path)
    {
        ChildrenResult children = await zookeeper.getChildrenAsync(path, false);
        List<string> sortedList = new List<string>(children.Children);
        sortedList.Sort();
        return sortedList;
    }

    /**
     * Given a parent path and a child node, create a combined full path
     *
     * @param parent the parent
     * @param child  the child
     * @return full path
     */
    public static String makePath(String parent, String child)
    {
        StringBuilder path = new StringBuilder();
        joinPath(path, parent, child);
        return path.ToString();
    }

    /**
     * Given a parent path and a list of children nodes, create a combined full path
     *
     * @param parent       the parent
     * @param firstChild   the first children in the path
     * @param restChildren the rest of the children in the path
     * @return full path
     */
    public static String makePath(String parent, String firstChild, params String[] restChildren)
    {
        StringBuilder path = new StringBuilder();
        joinPath(path, parent, firstChild);
        if (restChildren == null)
        {
            return path.ToString();
        }
        else
        {
            foreach(String child in restChildren)
            {
                joinPath(path, "", child);
            }
            return path.ToString();
        }
    }

    /**
     * Given a parent and a child node, join them in the given {@link StringBuilder path}
     *
     * @param path   the {@link StringBuilder} used to make the path
     * @param parent the parent
     * @param child  the child
     */
    private static void joinPath(StringBuilder path, String parent, String child)
    {
        // Add parent piece, with no trailing slash.
        if ((parent != null) && (parent.Length > 0))
        {
            if (!parent.StartsWith(PATH_SEPARATOR))
            {
                path.Append(PATH_SEPARATOR);
            }
            if (parent.EndsWith(PATH_SEPARATOR))
            {
                path.Append(parent.Substring(0, parent.Length - 1));
            }
            else
            {
                path.Append(parent);
            }
        }

        if ((child == null) || (child.Length == 0) || (child.Equals(PATH_SEPARATOR)))
        {
            // Special case, empty parent and child
            if (path.Length == 0)
            {
                path.Append(PATH_SEPARATOR);
            }
            return;
        }

        // Now add the separator between parent and child.
        path.Append(PATH_SEPARATOR);

        if (child.StartsWith(PATH_SEPARATOR))
        {
            child = child.Substring(1);
        }

        if (child.EndsWith(PATH_SEPARATOR))
        {
            child = child.Substring(0, child.Length - 1);
        }

        // Finally, add the child.
        path.Append(child);
    }

    private ZKPaths()
    {
    }

    private static CreateMode getCreateMode(bool asContainers)
    {
        return asContainers ? getContainerCreateMode() : CreateMode.PERSISTENT;
    }
}
}
