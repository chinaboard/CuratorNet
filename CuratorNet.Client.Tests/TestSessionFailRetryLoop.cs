using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CuratorNet.Test;
using NUnit.Framework;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Retry;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Atomics;

namespace CuratorNet.Client.Tests
{
    public class TestSessionFailRetryLoop : BaseZkTest
    {
        public TestSessionFailRetryLoop() 
            : base(ZkDefaultHosts, DefaultSessionTimeout, new NopWatcher(), false) {}

        [Test]
        public void testRetry()
        {
            CuratorZookeeperClient client = new CuratorZookeeperClient(ZkDefaultHosts, 
                                                                        DefaultSessionTimeout, 
                                                                        DefaultConnectionTimeout, 
                                                                        null, 
                                                                        new RetryOneTime(1));
            SessionFailRetryLoop retryLoop = client.newSessionFailRetryLoop(SessionFailRetryLoop.Mode.RETRY);
            retryLoop.start();
            try
            {
                client.start();
                AtomicBoolean     secondWasDone = new AtomicBoolean(false);
                AtomicBoolean     firstTime = new AtomicBoolean(true);
                while ( retryLoop.shouldContinue() )
                {
                    try
                    {
                        RetryLoop.callWithRetry
                            (
                             client,
                             CallableUtils.FromFunc<object>(() =>
                             {
                                 Task<Stat> existsTask;
                                 if (firstTime.compareAndSet(true, false))
                                 {
                                     existsTask = client.getZooKeeper().existsAsync("/foo/bar", false);
                                     existsTask.Wait();
                                     Assert.Null(existsTask.Result);
                                     KillSession.kill(client.getZooKeeper(), ZkDefaultHosts, DefaultSessionTimeout * 2);
                                     client.getZooKeeper();
                                     client.blockUntilConnectedOrTimedOut();
                                 }
                                 existsTask = client.getZooKeeper().existsAsync("/foo/bar", false);
                                 existsTask.Wait();
                                 Assert.Null(existsTask.Result);
                                 return null;
                             })
                        );

                        RetryLoop.callWithRetry
                        (
                            client,
                            CallableUtils.FromFunc<object>(() =>
                            {
                                Assert.False(firstTime.get());
                                Task<Stat> existsTask = client.getZooKeeper().existsAsync("/foo/bar", false);
                                existsTask.Wait();
                                Assert.Null(existsTask.Result);
                                secondWasDone.set(true);
                                return null;
                            })
                        );
                    }
                    catch ( Exception e )
                    {
                        retryLoop.takeException(e);
                    }
                }

                Assert.True(secondWasDone.get());
            }
            finally
            {
                retryLoop.Dispose();
                CloseableUtils.closeQuietly(client);
            }
        }

        [Test]
        public void testRetryStatic()
        {
            CuratorZookeeperClient client = new CuratorZookeeperClient(ZkDefaultHosts, 
                                                                        DefaultSessionTimeout, 
                                                                        DefaultConnectionTimeout, 
                                                                        null, 
                                                                        new RetryOneTime(1));
            SessionFailRetryLoop retryLoop = client.newSessionFailRetryLoop(SessionFailRetryLoop.Mode.RETRY);
            retryLoop.start();
                try
                {
                    client.start();
                    AtomicBoolean     secondWasDone = new AtomicBoolean(false);
                    AtomicBoolean     firstTime = new AtomicBoolean(true);
                    SessionFailRetryLoop.callWithRetry
                    (
                        client,
                        SessionFailRetryLoop.Mode.RETRY,
                        CallableUtils.FromFunc<object>(() =>
                        {
                            RetryLoop.callWithRetry(
                                    client,
                                    CallableUtils.FromFunc<object>(() =>
                                    {
                                        Task<Stat> existsTask;
                                        if ( firstTime.compareAndSet(true, false) )
                                        {
                                            existsTask = client.getZooKeeper().existsAsync("/foo/bar", false);
                                            existsTask.Wait();
                                            Assert.Null(existsTask.Result);
                                            KillSession.kill(client.getZooKeeper(), ZkDefaultHosts, DefaultSessionTimeout);
                                            client.getZooKeeper();
                                            client.blockUntilConnectedOrTimedOut();
                                        }
                                        existsTask = client.getZooKeeper().existsAsync("/foo/bar", false);
                                        existsTask.Wait();
                                        Assert.Null(existsTask.Result);
                                        return null;
                                    }
                            ));

                            RetryLoop.callWithRetry
                            (
                                client,
                                CallableUtils.FromFunc<object>(() =>
                                {
                                    Assert.False(firstTime.get());
                                    Task<Stat> existsTask = client.getZooKeeper().existsAsync("/foo/bar", false);
                                    existsTask.Wait();
                                    Assert.Null(existsTask.Result);
                                    secondWasDone.set(true);
                                    return null;
                                }
                            ));
                            return null;
                        }
                    ));

            Assert.True(secondWasDone.get());
        }
        finally
        {
            retryLoop.Dispose();
            CloseableUtils.closeQuietly(client);
        }
    }

    [Test]
    public void testBasic()
    {
        CuratorZookeeperClient    client = new CuratorZookeeperClient(ZkDefaultHosts, 
                                                                        DefaultSessionTimeout, 
                                                                        DefaultConnectionTimeout, 
                                                                        null, 
                                                                        new RetryOneTime(1));
        SessionFailRetryLoop retryLoop = client.newSessionFailRetryLoop(SessionFailRetryLoop.Mode.FAIL);
        retryLoop.start();
        try
        {
            client.start();
            try
            {
                while ( retryLoop.shouldContinue() )
                {
                    try
                    {
                        RetryLoop.callWithRetry
                        (
                            client,
                            CallableUtils.FromFunc<object>(() => 
                            {
                                Task<Stat> existsTask = client.getZooKeeper().existsAsync("/foo/bar", false);
                                existsTask.Wait();
                                Assert.Null(existsTask.Result);
                                KillSession.kill(client.getZooKeeper(), ZkDefaultHosts,DefaultSessionTimeout);

                                client.getZooKeeper();
                                client.blockUntilConnectedOrTimedOut();
                                existsTask = client.getZooKeeper().existsAsync("/foo/bar", false);
                                existsTask.Wait();
                                Assert.Null(existsTask.Result);
                                return null;
                            }
                        ));
                    }
                    catch ( Exception e )
                    {
                        retryLoop.takeException(e);
                    }
                }
                Assert.Fail();
            }
            catch ( SessionFailRetryLoop.SessionFailedException dummy )
            {
                // correct
            }
        }
        finally
        {
            retryLoop.Dispose();
            CloseableUtils.closeQuietly(client);
        }
    }

        [Test]
        public void testBasicStatic()
        {
            CuratorZookeeperClient    client = new CuratorZookeeperClient(ZkDefaultHosts, 
                                                                            DefaultSessionTimeout, 
                                                                            DefaultConnectionTimeout, 
                                                                            null, 
                                                                            new RetryOneTime(1));
            SessionFailRetryLoop retryLoop = client.newSessionFailRetryLoop(SessionFailRetryLoop.Mode.FAIL);
            retryLoop.start();
            try
            {
                client.start();
                try
                {
                    SessionFailRetryLoop.callWithRetry
                    (
                        client,
                        SessionFailRetryLoop.Mode.FAIL,
                        CallableUtils.FromFunc<object>(() => 
                        {
                            RetryLoop.callWithRetry
                            (
                                client,
                                CallableUtils.FromFunc<object>(() => 
                                {
                                    Task<Stat> existsTask = client.getZooKeeper().existsAsync("/foo/bar", false);
                                    existsTask.Wait();
                                    Assert.Null(existsTask.Result);
                                    KillSession.kill(client.getZooKeeper(), ZkDefaultHosts,DefaultSessionTimeout);

                                    client.getZooKeeper();
                                    client.blockUntilConnectedOrTimedOut();
                                    existsTask = client.getZooKeeper().existsAsync("/foo/bar", false);
                                    existsTask.Wait();
                                    Assert.Null(existsTask.Result);
                                    return null;
                                }
                            ));
                            return null;
                        }
                    ));
                }
                catch ( SessionFailRetryLoop.SessionFailedException dummy )
                {
                    // correct
                }
            }
            finally
            {
                retryLoop.Dispose();
                CloseableUtils.closeQuietly(client);
            }
        }
    }
}
