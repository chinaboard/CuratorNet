using System;
using System.Collections.Generic;
using System.Net;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Ensemble;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.CuratorNet.Framework.Imps;
using Org.Apache.Java.Types;

namespace Org.Apache.CuratorNet.Framework
{
    /**
     * Factory methods for creating framework-style clients
     */
    public class CuratorFrameworkFactory
    {
        private static readonly int DEFAULT_SESSION_TIMEOUT_MS = 60 * 1000;
        private static readonly int DEFAULT_CONNECTION_TIMEOUT_MS = 15 * 1000;
        private static readonly byte[] LOCAL_ADDRESS = getLocalAddress();
        private static readonly ICompressionProvider DEFAULT_COMPRESSION_PROVIDER = new GzipCompressionProvider();
        private static readonly DefaultZookeeperFactory DEFAULT_ZOOKEEPER_FACTORY = new DefaultZookeeperFactory();
        private static readonly DefaultACLProvider DEFAULT_ACL_PROVIDER = new DefaultACLProvider();
        private static readonly long DEFAULT_INACTIVE_THRESHOLD_MS = 3 * 60 * 1000;
        private static readonly int DEFAULT_CLOSE_WAIT_MS = 1 * 60 * 1000;

        /**
         * Return a new builder that builds a CuratorFramework
         *
         * @return new builder
         */
        public static Builder builder()
        {
            return new Builder();
        }

        /**
         * Create a new client with default session timeout and default connection timeout
         *
         * @param connectString list of servers to connect to
         * @param retryPolicy   retry policy to use
         * @return client
         */
        public static CuratorFramework newClient(String connectString, IRetryPolicy retryPolicy)
        {
            return newClient(connectString, DEFAULT_SESSION_TIMEOUT_MS, DEFAULT_CONNECTION_TIMEOUT_MS, retryPolicy);
        }

        /**
         * Create a new client
         *
         * @param connectString       list of servers to connect to
         * @param sessionTimeoutMs    session timeout
         * @param connectionTimeoutMs connection timeout
         * @param retryPolicy         retry policy to use
         * @return client
         */
        public static CuratorFramework newClient(String connectString, int sessionTimeoutMs, int connectionTimeoutMs, IRetryPolicy retryPolicy)
        {
            return builder().
                ConnectString(connectString).
                SessionTimeoutMs(sessionTimeoutMs).
                ConnectionTimeoutMs(connectionTimeoutMs).
                RetryPolicy(retryPolicy).
                build();
        }

        /**
         * Return the local address as bytes that can be used as a node payload
         *
         * @return local address bytes
         */
        public static byte[] getLocalAddress()
        {
            try
            {
                IPAddress[] ipAddresses = Dns.GetHostAddresses(Dns.GetHostName());
                return ipAddresses[0].GetAddressBytes();
            }
            catch (Exception)
            {
                // ignore
            }
            return new byte[0];
        }

        public class Builder
        {
            private IEnsembleProvider ensembleProvider;
            private int sessionTimeoutMs = DEFAULT_SESSION_TIMEOUT_MS;
            private int connectionTimeoutMs = DEFAULT_CONNECTION_TIMEOUT_MS;
            private int maxCloseWaitMs = DEFAULT_CLOSE_WAIT_MS;
            private int maxEventQueueSize = 25;
            private IRetryPolicy retryPolicy;
            private String @namespace;
            private List<AuthInfo> authInfos = null;
            private byte[] defaultDataBuffer = LOCAL_ADDRESS;
            private ICompressionProvider compressionProvider = DEFAULT_COMPRESSION_PROVIDER;
            private IZookeeperFactory zookeeperFactory = DEFAULT_ZOOKEEPER_FACTORY;
            private IACLProvider aclProvider = DEFAULT_ACL_PROVIDER;
            private bool canBeReadOnly = false;
            private bool useContainerParentsIfAvailable = true;

            /**
             * Apply the current values and build a new CuratorFramework
             *
             * @return new CuratorFramework
             */
            public CuratorFramework build()
            {
                return new CuratorFrameworkImpl(this);
            }

            /**
             * Apply the current values and build a new temporary CuratorFramework. Temporary
             * CuratorFramework instances are meant for single requests to ZooKeeper ensembles
             * over a failure prone network such as a WAN. The APIs available from {@link CuratorTempFramework}
             * are limited. Further, the connection will be closed after 3 minutes of inactivity.
             *
             * @return temp instance
             */
            public CuratorTempFramework buildTemp()
            {
                return buildTemp(DEFAULT_INACTIVE_THRESHOLD_MS);
            }

            /**
             * Apply the current values and build a new temporary CuratorFramework. Temporary
             * CuratorFramework instances are meant for single requests to ZooKeeper ensembles
             * over a failure prone network such as a WAN. The APIs available from {@link CuratorTempFramework}
             * are limited. Further, the connection will be closed after <code>inactiveThresholdMs</code> milliseconds of inactivity.
             *
             * @param inactiveThreshold number of milliseconds of inactivity to cause connection close
             * @param unit              threshold unit
             * @return temp instance
             */
            public CuratorTempFramework buildTemp(long inactiveThresholdMs)
            {
                return new CuratorTempFrameworkImpl(this, inactiveThresholdMs);
            }

            /**
             * Add connection authorization
             * 
             * Subsequent calls to this method overwrite the prior calls.
             *
             * @param scheme the scheme
             * @param auth   the auth bytes
             * @return this
             */
            public Builder Authorization(String scheme, byte[] auth)
            {
                byte[] authCopy = null;
                if (auth != null)
                {
                    authCopy = new byte[auth.Length];
                    Array.Copy(auth, authCopy, auth.Length);
                }
                var authInfoList = new List<AuthInfo>();
                authInfoList.Add(new AuthInfo(scheme, authCopy));
                return Authorization(authInfoList);
            }

            /**
             * Add connection authorization. The supplied authInfos are appended to those added via call to
             * {@link #authorization(java.lang.String, byte[])} for backward compatibility.
             * <p/>
             * Subsequent calls to this method overwrite the prior calls.
             *
             * @param authInfos list of {@link AuthInfo} objects with scheme and auth
             * @return this
             */
            public Builder Authorization(List<AuthInfo> authInfos)
            {
                this.authInfos = new List<AuthInfo>(authInfos);
                return this;
            }

            public Builder EventQueueSize(int queueSize)
            {
                if (queueSize <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(queueSize));
                }
                maxEventQueueSize = queueSize;
                return this;
            }

            /**
             * Set the list of servers to connect to. IMPORTANT: use either this or {@link #ensembleProvider(EnsembleProvider)}
             * but not both.
             *
             * @param connectString list of servers to connect to
             * @return this
             */
            public Builder ConnectString(String connectString)
            {
                ensembleProvider = new FixedEnsembleProvider(connectString);
                return this;
            }

            /**
             * Set the list ensemble provider. IMPORTANT: use either this or {@link #connectString(String)}
             * but not both.
             *
             * @param ensembleProvider the ensemble provider to use
             * @return this
             */
            public Builder EnsembleProvider(IEnsembleProvider ensembleProvider)
            {
                this.ensembleProvider = ensembleProvider;
                return this;
            }

            /**
             * Sets the data to use when {@link PathAndBytesable#forPath(String)} is used.
             * This is useful for debugging purposes. For example, you could set this to be the IP of the
             * client.
             *
             * @param defaultData new default data to use
             * @return this
             */
            public Builder DefaultData(byte[] defaultData)
            {
                if (defaultData != null)
                {
                    defaultDataBuffer = new byte[defaultData.Length];
                    Array.Copy(defaultDataBuffer, defaultData, defaultDataBuffer.Length);
                }
                else
                {
                    defaultDataBuffer = null;
                }
//                this.defaultData = (defaultData != null) ? Arrays.copyOf(defaultData, defaultData.length) : null;
                return this;
            }

            /**
             * As ZooKeeper is a shared space, users of a given cluster should stay within
             * a pre-defined namespace. If a namespace is set here, all paths will get pre-pended
             * with the namespace
             *
             * @param namespace the namespace
             * @return this
             */
            public Builder Namespace(String @namespace)
            {
                this.@namespace = @namespace;
                return this;
            }

            /**
             * @param sessionTimeoutMs session timeout
             * @return this
             */
            public Builder SessionTimeoutMs(int sessionTimeoutMs)
            {
                this.sessionTimeoutMs = sessionTimeoutMs;
                return this;
            }

            /**
             * @param connectionTimeoutMs connection timeout
             * @return this
             */
            public Builder ConnectionTimeoutMs(int connectionTimeoutMs)
            {
                this.connectionTimeoutMs = connectionTimeoutMs;
                return this;
            }

            /**
             * @param maxCloseWaitMs time to wait during close to join background threads
             * @return this
             */
            public Builder MaxCloseWaitMs(int maxCloseWaitMs)
            {
                this.maxCloseWaitMs = maxCloseWaitMs;
                return this;
            }

            /**
             * @param retryPolicy retry policy to use
             * @return this
             */
            public Builder RetryPolicy(IRetryPolicy retryPolicy)
            {
                this.retryPolicy = retryPolicy;
                return this;
            }

            /**
             * @param compressionProvider the compression provider
             * @return this
             */
            public Builder CompressionProvider(ICompressionProvider compressionProvider)
            {
                this.compressionProvider = compressionProvider;
                return this;
            }

            /**
             * @param zookeeperFactory the zookeeper factory to use
             * @return this
             */
            public Builder ZookeeperFactory(IZookeeperFactory zookeeperFactory)
            {
                this.zookeeperFactory = zookeeperFactory;
                return this;
            }

            /**
             * @param aclProvider a provider for ACLs
             * @return this
             */
            public Builder AclProvider(IACLProvider aclProvider)
            {
                this.aclProvider = aclProvider;
                return this;
            }

            /**
             * @param canBeReadOnly if true, allow ZooKeeper client to enter
             *                      read only mode in case of a network partition. See
             *                      {@link ZooKeeper#ZooKeeper(String, int, Watcher, long, byte[], boolean)}
             *                      for details
             * @return this
             */
            public Builder CanBeReadOnly(bool canBeReadOnly)
            {
                this.canBeReadOnly = canBeReadOnly;
                return this;
            }

            /**
             * By default, Curator uses {@link CreateBuilder#creatingParentContainersIfNeeded()}
             * if the ZK JAR supports {@link CreateMode#CONTAINER}. Call this method to turn off this behavior.
             *
             * @return this
             */
            public Builder DontUseContainerParents()
            {
                this.useContainerParentsIfAvailable = false;
                return this;
            }

            public int GetEventQueueSize()
            {
                return maxEventQueueSize;
            }

            public IACLProvider GetAclProvider()
            {
                return aclProvider;
            }

            public IZookeeperFactory GetZookeeperFactory()
            {
                return zookeeperFactory;
            }

            public ICompressionProvider GetCompressionProvider()
            {
                return compressionProvider;
            }

            public IEnsembleProvider GetEnsembleProvider()
            {
                return ensembleProvider;
            }

            public int GetSessionTimeoutMs()
            {
                return sessionTimeoutMs;
            }

            public int GetConnectionTimeoutMs()
            {
                return connectionTimeoutMs;
            }

            public int GetMaxCloseWaitMs()
            {
                return maxCloseWaitMs;
            }

            public IRetryPolicy GetRetryPolicy()
            {
                return retryPolicy;
            }

            public String GetNamespace()
            {
                return @namespace;
            }

            public bool UseContainerParentsIfAvailable()
            {
                return useContainerParentsIfAvailable;
            }

            public String GetAuthScheme()
            {
                int qty = (authInfos != null) ? authInfos.Count : 0;
                switch (qty)
                {
                    case 0:
                    {
                        return null;
                    }
                    case 1:
                    {
                        return authInfos[0].getScheme();
                    }
                    default:
                    {
                        throw new InvalidOperationException("More than 1 auth has been added");
                    }
                }
            }

            [Obsolete]
            public byte[] GetAuthValue()
            {
                int qty = (authInfos != null) ? authInfos.Count : 0;
                switch (qty)
                {
                    case 0:
                        {
                            return null;
                        }

                    case 1:
                        {
                            byte[] bytes = authInfos[0].getAuth();
                            if (bytes == null)
                            {
                                return null;
                            }
                            byte[] copy = new byte[bytes.Length];
                            Array.Copy(bytes, copy, bytes.Length);
                            return copy;
                        }

                    default:
                        {
                            throw new InvalidOperationException("More than 1 auth has been added");
                        }
                }
            }

            public List<AuthInfo> GetAuthInfos()
            {
                return authInfos;
            }

            public byte[] GetDefaultData()
            {
                return defaultDataBuffer;
            }

            public bool CanBeReadOnly()
            {
                return canBeReadOnly;
            }

            internal Builder()
            {
            }
        }

        private CuratorFrameworkFactory()
        {
        }
    }
}