using System;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.CuratorNet.Framework.API.Transaction;
using Org.Apache.CuratorNet.Framework.Imps;
using Org.Apache.CuratorNet.Framework.Listen;
using Org.Apache.CuratorNet.Framework.State;
using Org.Apache.Java.Types;

namespace Org.Apache.CuratorNet.Framework
{
    /**
     * Zookeeper framework-style client
     */
    public interface CuratorFramework : IDisposable
    {
        /**
         * Start the client. Most mutator methods will not work until the client is started
         */
        void start();

        /**
         * Stop the client
         */
        void close();

        /**
         * Returns the state of this instance
         *
         * @return state
         */
        CuratorFrameworkState getState();

        /**
         * Return true if the client is started, not closed, etc.
         *
         * @return true/false
         * @deprecated use {@link #getState()} instead
         */
        bool isStarted();

        /**
         * Start a create builder
         *
         * @return builder object
         */
        ICreateBuilder create();

        /**
         * Start a delete builder
         *
         * @return builder object
         */
        DeleteBuilder delete();

        /**
         * Start an exists builder
         * <p>
         * The builder will return a Stat object as if org.apache.zookeeper.ZooKeeper.exists() were called.  Thus, a null
         * means that it does not exist and an actual Stat object means it does exist.
         *
         * @return builder object
         */
        IExistsBuilder checkExists();

        /**
         * Start a get data builder
         *
         * @return builder object
         */
        IGetDataBuilder getData();

        /**
         * Start a set data builder
         *
         * @return builder object
         */
        ISetDataBuilder setData();

        /**
         * Start a get children builder
         *
         * @return builder object
         */
        IGetChildrenBuilder getChildren();

        /**
         * Start a get ACL builder
         *
         * @return builder object
         */
        IGetACLBuilder getACL();

        /**
         * Start a set ACL builder
         *
         * @return builder object
         */
        ISetACLBuilder setACL();

        /**
         * Start a transaction builder
         *
         * @return builder object
         */
        ICuratorTransaction inTransaction();

        /**
         * Perform a sync on the given path - syncs are always in the background
         *
         * @param path                    the path
         * @param backgroundContextObject optional context
         * @deprecated use {@link #sync()} instead
         */
        [Obsolete]
        void sync(String path, Object backgroundContextObject);

        /**
         * Create all nodes in the specified path as containers if they don't
         * already exist
         *
         * @param path path to create
         * @throws Exception errors
         */
        void createContainers(String path);

        /**
         * Start a sync builder. Note: sync is ALWAYS in the background even
         * if you don't use one of the background() methods
         *
         * @return builder object
         */
        ISyncBuilder sync();

        /**
         * Returns the listenable interface for the Connect State
         *
         * @return listenable
         */
        Listenable<IConnectionStateListener> getConnectionStateListenable();

        /**
         * Returns the listenable interface for events
         *
         * @return listenable
         */
        Listenable<ICuratorListener> getCuratorListenable();

        /**
         * Returns the listenable interface for unhandled errors
         *
         * @return listenable
         */
        Listenable<IUnhandledErrorListener> getUnhandledErrorListenable();

        /**
         * Returns a facade of the current instance that does _not_ automatically
         * pre-pend the namespace to all paths
         *
         * @return facade
         * @deprecated Since 2.9.0 - use {@link #usingNamespace} passing <code>null</code>
         */
        [Obsolete]
        CuratorFramework nonNamespaceView();

        /**
         * Returns a facade of the current instance that uses the specified namespace
         * or no namespace if <code>newNamespace</code> is <code>null</code>.
         *
         * @param newNamespace the new namespace or null for none
         * @return facade
         */
        CuratorFramework usingNamespace(String newNamespace);

        /**
         * Return the current namespace or "" if none
         *
         * @return namespace
         */
        String getNamespace();

        /**
         * Return the managed zookeeper client
         *
         * @return client
         */
        CuratorZookeeperClient getZookeeperClient();

        /**
         * Allocates an ensure path instance that is namespace aware
         *
         * @param path path to ensure
         * @return new EnsurePath instance
         * @deprecated Since 2.9.0 - prefer {@link CreateBuilder#creatingParentContainersIfNeeded()}, {@link ExistsBuilder#creatingParentContainersIfNeeded()}
         * or {@link CuratorFramework#createContainers(String)}
         */
        [Obsolete]
        EnsurePath newNamespaceAwareEnsurePath(String path);

        /**
         * Curator can hold internal references to watchers that may inhibit garbage collection.
         * Call this method on watchers you are no longer interested in.
         *
         * @param watcher the watcher
         */
        void clearWatcherReferences(Watcher watcher);

        /**
         * Block until a connection to ZooKeeper is available or the maxWaitTime has been exceeded
         * @param maxWaitTime The maximum wait time. Specify a value &lt;= 0 to wait indefinitely
         * @param units The time units for the maximum wait time.
         * @return True if connection has been established, false otherwise.
         * @throws InterruptedException If interrupted while waiting
         */
        bool blockUntilConnected(int maxWaitTime, TimeUnit units);

        /**
         * Block until a connection to ZooKeeper is available. This method will not return until a
         * connection is available or it is interrupted, in which case an InterruptedException will
         * be thrown
         * @throws InterruptedException If interrupted while waiting
         */
        void blockUntilConnected();
    }
}
