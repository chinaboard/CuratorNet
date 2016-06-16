using System;
using System.Threading;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.CuratorNet.Framework.API.Transaction;
using Org.Apache.Java.Types;
using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    public class CuratorTempFrameworkImpl : CuratorTempFramework
    {
        private readonly CuratorFrameworkFactory.Builder factory;
        private readonly long inactiveThresholdMs;

        // guarded by sync
        private CuratorFrameworkImpl client;

        // guarded by sync
        private IScheduledExecutorService cleanup;

        // guarded by sync
        private long lastAccess;
        private readonly object _openConnectionLock = new object();
        private readonly object _checkInactiveLock = new object();
        private readonly object _closeLock = new object();

        public CuratorTempFrameworkImpl(CuratorFrameworkFactory.Builder factory, long inactiveThresholdMs)
        {
            this.factory = factory;
            this.inactiveThresholdMs = inactiveThresholdMs;
        }

        public void close()
        {
            closeClient();
        }

        public ICuratorTransaction inTransaction()
        {
            openConnectionIfNeeded();
            return new CuratorTransactionImpl(client);
        }

        public ITempGetDataBuilder getData()
        {
            openConnectionIfNeeded();
            return new TempGetDataBuilderImpl(client);
        }

        internal CuratorFrameworkImpl getClient()
        {
            return client;
        }

        internal IScheduledExecutorService getCleanup()
        {
            return cleanup;
        }

        void updateLastAccess()
        {
            Volatile.Write(ref lastAccess, GetCurrentMs());
        }

        private static long GetCurrentMs()
        {
            return DateTime.Now.Ticks / 1000;
        }

        private void openConnectionIfNeeded()
        {
            lock (_openConnectionLock)
            {
                if ( client == null )
                {
                    client = (CuratorFrameworkImpl)factory.build(); // cast is safe - we control both sides of this
                    client.start();
                }
                cleanup = ThreadUtils.newSingleThreadScheduledExecutor("CuratorTempFrameworkImpl");

                IRunnable command = RunnableUtils.FromFunc(checkInactive);
                cleanup.scheduleAtFixedRate(command, inactiveThresholdMs, inactiveThresholdMs);
                updateLastAccess();
            }
        }

        private void checkInactive()
        {
            lock (_checkInactiveLock)
            {
                long elapsed = GetCurrentMs() - lastAccess;
                if (elapsed >= inactiveThresholdMs)
                {
                    closeClient();
                }
            }
        }

        private void closeClient()
        {
            lock (_closeLock)
            {
                if (cleanup != null)
                {
                    cleanup.shutdownNow();
                    cleanup = null;
                }

                if (client != null)
                {
                    CloseableUtils.closeQuietly(client);
                    client = null;
                }
            }
        }
    }
}