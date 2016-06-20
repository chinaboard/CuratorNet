using System;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Atomics;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class NamespaceImpl
    {
        private readonly CuratorFrameworkImpl client;
        private readonly string @namespace;
        private readonly AtomicBoolean ensurePathNeeded;

        internal NamespaceImpl(CuratorFrameworkImpl client, String @namespace)
        {
            if ( @namespace != null )
            {
                try
                {
                    PathUtils.validatePath("/" + @namespace);
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException("Invalid namespace: " + @namespace);
                }
            }

            this.client = client;
            this.@namespace = @namespace;
            ensurePathNeeded = new AtomicBoolean(@namespace != null);
        }

        internal String getNamespace()
        {
            return @namespace;
        }

        internal String unfixForNamespace(String path)
        {
            if ((@namespace != null) && (path != null) )
            {
                String namespacePath = ZKPaths.makePath(@namespace, null);
                if ( path.StartsWith(namespacePath) )
                {
                    path = (path.Length > namespacePath.Length) ? path.Substring(namespacePath.Length) : "/";
                }
        }
            return path;
        }

        internal String fixForNamespace(String path, bool isSequential)
        {
            if (ensurePathNeeded.get())
            {
                try
                {
                    CuratorZookeeperClient zookeeperClient = client.getZookeeperClient();
                    RetryLoop.callWithRetry
                    (
                        zookeeperClient,
                        CallableUtils.FromFunc<object>(() =>
                        {
                            ZKPaths.mkdirs(zookeeperClient.getZooKeeper(), 
                                            ZKPaths.makePath("/", @namespace), 
                                            true, 
                                            client.getAclProvider(), 
                                            true);
                            return null;
                        })
                    );
                    ensurePathNeeded.set(false);
                }
                catch ( Exception e )
                {
                    ThreadUtils.checkInterrupted(e);
                    client.logError("Ensure path threw exception", e);
                }
            }

            return ZKPaths.fixForNamespace(@namespace, path, isSequential);
        }

        internal EnsurePath newNamespaceAwareEnsurePath(String path)
        {
            return new EnsurePath(fixForNamespace(path, false), client.getAclProvider());
        }
    }
}