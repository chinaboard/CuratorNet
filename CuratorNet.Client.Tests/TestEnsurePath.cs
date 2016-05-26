using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Ensemble;
using Org.Apache.CuratorNet.Client.Retry;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.Java.Types;
using Org.Apache.Java.Types.Concurrent;

namespace CuratorNet.Client.Tests
{
    [TestFixture]
    public class TestEnsurePath : BaseZkTest
    {
        public TestEnsurePath() 
            : base(ZkDefaultHosts, DefaultSessionTimeout, null, false) {}

        [Test]
        public void testBasic()
        {
            IRetryPolicy retryPolicy = new RetryOneTime(1);
            EnsurePath ensurePath = new EnsurePath("/one/two/three");
            CuratorZookeeperClient curator 
                = new CuratorZookeeperClient(new FixedEnsembleProvider(ZkDefaultHosts), 
                                                DefaultSessionTimeout,
                                                DefaultConnectionTimeout,
                                                null,
                                                retryPolicy);
            curator.start();
            ensurePath.ensure(curator);
        }

//        [Test]
//        public void testSimultaneous()
//        {
////            ZooKeeper client = mock(ZooKeeper.class, Mockito.RETURNS_MOCKS);
//            IRetryPolicy retryPolicy = new RetryOneTime(1);
//            RetryLoop retryLoop = new RetryLoop(retryPolicy, null);
//            CuratorZookeeperClient curator
//                = new CuratorZookeeperClient(new FixedEnsembleProvider(ZkDefaultHosts),
//                                                DefaultSessionTimeout,
//                                                DefaultConnectionTimeout,
//                                                null,
//                                                retryPolicy);
//            //            CuratorZookeeperClient  curator = mock(CuratorZookeeperClient.class);
//            //            when(curator.getZooKeeper()).thenReturn(client);
//            //            when(curator.getRetryPolicy()).thenReturn(retryPolicy);
//            //            when(curator.newRetryLoop()).thenReturn(retryLoop);
//
////            Stat              fakeStat = mock(Stat.class);
//            Barrier startedLatch = new Barrier(3);
//            Barrier finishedLatch = new Barrier(3);
//            Semaphore semaphore = new Semaphore(0,3);
//            when(client.exists(Mockito.<String>any(), anyBoolean())).thenAnswer
//            (
//                new Answer<Stat>()
//                {
//                    @Override
//                    public Stat answer(InvocationOnMock invocation) throws Throwable
//                    {
//                        semaphore.acquire();
//                                    return fakeStat;
//                    }
//                    }
//            );
//
//            EnsurePath ensurePath = new EnsurePath("/one/two/three");
//            IExecutorService service = new TaskExecutorService();
//            for ( int i = 0; i< 2; ++i )
//            {
//                service.submit
//                (
//                    new Callable<Void>()
//                    {
//                        @Override
//                        public Void call() throws Exception
//                        {
//                            startedLatch.countDown();
//                            ensurePath.ensure(curator);
//                            finishedLatch.countDown();
//                                                return null;
//                        }
//                    }
//                );
//            }
//
//            Assert.assertTrue(startedLatch.await(10, TimeUnit.SECONDS));
//            semaphore.release(3);
//            Assert.assertTrue(finishedLatch.await(10, TimeUnit.SECONDS));
//            verify(client, times(3)).exists(Mockito.<String>any(), anyBoolean());
//
//            ensurePath.ensure(curator);
//            verifyNoMoreInteractions(client);
//            ensurePath.ensure(curator);
//            verifyNoMoreInteractions(client);
//        }
    }
}
