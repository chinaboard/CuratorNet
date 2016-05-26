using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CuratorNet.Test;
using NUnit.Framework;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Ensemble;
using Org.Apache.CuratorNet.Client.Retry;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Atomics;

namespace CuratorNet.Client.Tests
{
    [TestFixture]
    public class BasicTests : BaseZkTest
    {
        public BasicTests() : base(ZkDefaultHosts, DefaultSessionTimeout, DefaultWatcher, false) { }

        [Test]
        public void testFactory()
        {
            IZookeeperFactory zookeeperFactory = new SingleInstanceZkFactory(Zookeeper);
            CuratorZookeeperClient client 
                = new CuratorZookeeperClient(zookeeperFactory, 
                                                new FixedEnsembleProvider(ZkDefaultHosts),
                                                DefaultSessionTimeout,
                                                DefaultConnectionTimeout, 
                                                null, 
                                                new RetryOneTime(1),
                                                false);
            client.start();
            Assert.AreEqual(client.getZooKeeper(), Zookeeper);
        }

        class SingleInstanceZkFactory : IZookeeperFactory
        {
            private readonly ZooKeeper _zookeeper;

            public SingleInstanceZkFactory(ZooKeeper zookeeper)
            {
                _zookeeper = zookeeper;
            }

            public ZooKeeper newZooKeeper(String connectString, 
                                            int sessionTimeout, 
                                            Watcher watcher, 
                                            bool canBeReadOnly)
            {
                return _zookeeper;
            }
        }

        [Test]
        public void testExpiredSession()
        {
            Barrier latch = new Barrier(2);
            Watcher watcher = new ExpiredWatcher(latch);
            CuratorZookeeperClient client = new CuratorZookeeperClient(ZkDefaultHosts, 
                                                                        DefaultSessionTimeout,
                                                                        DefaultConnectionTimeout, 
                                                                        watcher, 
                                                                        new RetryOneTime(2));
            client.start();
            try
            {
                AtomicBoolean firstTime = new AtomicBoolean(true);
                RetryLoop.callWithRetry(
                    client,
                    CallableUtils.FromFunc<object>(() =>
                    {
                        if (firstTime.compareAndSet(false, true))
                        {
                            try
                            {
                                Task<string> createTask = client.getZooKeeper()
                                                                .createAsync("/foo", 
                                                                                new byte[0], 
                                                                                ZooDefs.Ids.OPEN_ACL_UNSAFE, 
                                                                                CreateMode.PERSISTENT);
                                createTask.Wait();
                            }
                            catch (AggregateException e)
                            {
                                if (e.InnerException is KeeperException.NodeExistsException)
                                {
                                    // ignore
                                }
                                else
                                {
                                    throw e;
                                }
                            }

                            KillSession.kill(client.getZooKeeper(), ZkDefaultHosts, DefaultSessionTimeout);
                            Assert.True(latch.SignalAndWait(DefaultSessionTimeout));
                        }
                        ZooKeeper zooKeeper = client.getZooKeeper();
                        client.blockUntilConnectedOrTimedOut();
                        Task<Stat> task = zooKeeper.existsAsync("/foo", false);
                        task.Wait();
                        Stat stat = task.Result;
                        Assert.NotNull(stat);
                        Assert.Greater(stat.getCzxid(), 0);
                        return null;
                    })
                );
            }
            finally
            {
                client.Dispose();
            }
        }

        //        [Test]
        //        public void testReconnect()
        //        {
        //            CuratorZookeeperClient client = new CuratorZookeeperClient(ZkDefaultHosts, 
        //                                                                        10000, 
        //                                                                        10000, 
        //                                                                        null, 
        //                                                                        new RetryOneTime(1));
        //            client.start();
        //            try
        //            {
        //                client.blockUntilConnectedOrTimedOut();
        //                byte[] writtenData = { 1, 2, 3 };
        //                client.getZooKeeper().createAsync("/test", 
        //                                                    writtenData, 
        //                                                    ZooDefs.Ids.OPEN_ACL_UNSAFE, 
        //                                                    CreateMode.PERSISTENT)
        //                                     .Wait();
        //                Thread.Sleep(1000);
        //                server.stop();
        //                Thread.Sleep(1000);
        //
        //                server.restart();
        //                Assert.True(client.blockUntilConnectedOrTimedOut());
        //                Task<DataResult> dataAsync = client.getZooKeeper().getDataAsync("/test", false);
        //                dataAsync.Wait();
        //                byte[] readData = dataAsync.Result.Data;
        //                Assert.AreEqual(readData, writtenData);
        //            }
        //            finally
        //            {
        //                client.Dispose();
        //            }
        //        }

        [Test]
        public void testSimple()
        {
            CuratorZookeeperClient client = new CuratorZookeeperClient(ZkDefaultHosts, 
                                                                        10000, 
                                                                        10000, 
                                                                        null, 
                                                                        new RetryOneTime(1));
            client.start();
            try
            {
                client.blockUntilConnectedOrTimedOut();
                Task<string> pathTask = client.getZooKeeper().createAsync("/test", 
                                                                new byte[] { 1, 2, 3 }, 
                                                                ZooDefs.Ids.OPEN_ACL_UNSAFE, 
                                                                CreateMode.PERSISTENT);
                pathTask.Wait();
                Assert.AreEqual(pathTask.Result, "/test");
            }
            finally
            {
                client.Dispose();
            }
        }

        [Test]
        public void testBackgroundConnect()
        {
            int CONNECTION_TIMEOUT_MS = 4000;

            CuratorZookeeperClient client = new CuratorZookeeperClient(ZkDefaultHosts, 
                                                                        10000, 
                                                                        CONNECTION_TIMEOUT_MS, 
                                                                        null, 
                                                                        new RetryOneTime(1));
            try
            {
                Assert.False(client.isConnected());
                client.start();
                bool outerMustContinue = false;
                do
                {
                    for ( int i = 0; i < (CONNECTION_TIMEOUT_MS / 1000); ++i )
                    {
                        if ( client.isConnected() )
                        {
                            outerMustContinue = true;
                            break;
                        }
                        Thread.Sleep(CONNECTION_TIMEOUT_MS);
                    }
                    if (outerMustContinue)
                    {
                        continue;
                    }
                    Assert.Fail();
                } while ( false );
            }
            finally
            {
                client.Dispose();
            }
        }
    }
}
