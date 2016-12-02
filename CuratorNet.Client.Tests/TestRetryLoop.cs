using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Retry;

namespace CuratorNet.Client.Tests
{
    [TestFixture]
    public class TestRetryLoop : BaseZkTest
    {
        public TestRetryLoop()
            : base(ZkDefaultHosts, DefaultSessionTimeout, null, false) { }

        public class RetrySleeper : IRetrySleeper
        {
            public void sleepFor(int timeMs)
            {
                Assert.True(timeMs <= 100);
            }
        }

        [Test]
        public void testExponentialBackoffRetryLimit()
        {
            IRetrySleeper sleeper = new RetrySleeper();
            ExponentialBackoffRetry retry = new ExponentialBackoffRetry(1, Int32.MaxValue, 100);
            for (int i = 0; i >= 0; ++i)
            {
                retry.allowRetry(i, 0, sleeper);
            }
        }

        [Test]
        public void testRetryLoopWithFailure()
        {
            CuratorZookeeperClient client = new CuratorZookeeperClient(ZkDefaultHosts,
                                                                        DefaultSessionTimeout,
                                                                        DefaultConnectionTimeout,
                                                                        null,
                                                                        new RetryOneTime(1));
            client.start();
            try
            {
                int loopCount = 0;
                RetryLoop retryLoop = client.newRetryLoop();
                while (retryLoop.shouldContinue())
                {
                    ++loopCount;
                    switch (loopCount)
                    {
                        case 1:
                            {
                                //                            retryLoop.takeException();
                                break;
                            }

                        case 2:
                            {
                                retryLoop.markComplete();
                                break;
                            }

                        case 3:
                        case 4:
                            {
                                // ignore
                                break;
                            }

                        default:
                            {
                                Assert.Fail();
                                break;
                            }
                    }
                }

                Assert.True(loopCount >= 2);
            }
            finally
            {
                client.Dispose();
            }
        }

        [Test]
        public async Task testRetryLoop()
        {
            CuratorZookeeperClient client = new CuratorZookeeperClient(ZkDefaultHosts,
                                                                        DefaultSessionTimeout,
                                                                        DefaultConnectionTimeout,
                                                                        null,
                                                                        new RetryOneTime(1));
            client.start();
            try
            {
                int loopCount = 0;
                RetryLoop retryLoop = client.newRetryLoop();
                while (retryLoop.shouldContinue())
                {
                    if (++loopCount > 2)
                    {
                        Assert.Fail();
                        break;
                    }

                    try
                    {
                        var path = "/test";
                        if (await client.getZooKeeper().existsAsync(path, false) != null)
                        {
                            client.getZooKeeper().deleteAsync(path).Wait();
                        }

                        client.getZooKeeper().createAsync(path,
                                                            new byte[] { 1, 2, 3 },
                                                            ZooDefs.Ids.OPEN_ACL_UNSAFE,
                                                            CreateMode.EPHEMERAL)
                                             .Wait();
                        retryLoop.markComplete();
                    }
                    catch (Exception e)
                    {
                        retryLoop.takeException(e);
                    }
                }

                Assert.True(loopCount > 0);
            }
            finally
            {
                client.Dispose();
            }
        }

        [Test]
        public void testRetryForever()
        {
            int retryIntervalMs = 1;
            Mock<IRetrySleeper> sleeper = new Mock<IRetrySleeper>();
            RetryForever retryForever = new RetryForever(retryIntervalMs);

            for (int i = 0; i < 10; i++)
            {
                bool allowed = retryForever.allowRetry(i, 0, sleeper.Object);
                Assert.True(allowed);
                sleeper.Verify(retrySleeper => retrySleeper.sleepFor(retryIntervalMs),
                                Times.Exactly(i + 1));
            }
        }
    }
}
