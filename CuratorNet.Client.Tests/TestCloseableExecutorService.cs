using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.Java.Types;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace CuratorNet.Client.Tests
{
    [TestFixture]
    public class TestCloseableExecutorService
    {
        private const int QTY = 10;

        private volatile IExecutorService executorService;

        [SetUp]
        public void setup()
        {
            executorService = ThreadUtils.newFixedThreadPool(QTY * 2, "testPool");
        }

        [TearDown]
        public void tearDown()
        {
            executorService.Dispose();
        }

        [Test]
        public void testBasicRunnable()
        {
            try
            {
                CloseableExecutorService service = new CloseableExecutorService(executorService);
                CountdownEvent startLatch = new CountdownEvent(QTY);
                CountdownEvent latch = new CountdownEvent(QTY);
                for (int i = 0; i < QTY; i++)
                {
                    submitRunnable(service, startLatch, latch);
                }
                Assert.True(startLatch.Wait(TimeSpan.FromSeconds(15)));
                service.Dispose();
                Assert.True(latch.Wait(TimeSpan.FromSeconds(15)));
            }
            catch ( AssertionException e )
            {
                throw e;
            }
            catch ( Exception e )
            {
                Console.WriteLine(e.ToString());
            }
        }

        [Test]
        public void testBasicCallable()
        {
            CloseableExecutorService service = new CloseableExecutorService(executorService);
            CountdownEvent startLatch = new CountdownEvent(QTY);
            CountdownEvent latch = new CountdownEvent(QTY);
            for (int i = 0; i < QTY; i++)
            {
                submitJoinRunnable(service, startLatch, latch);
            }
            Assert.True(startLatch.Wait(TimeSpan.FromSeconds(15)));
            service.Dispose();
            Assert.True(latch.Wait(TimeSpan.FromSeconds(15)));
        }

        [Test]
        public void testListeningRunnable()
        {
            CloseableExecutorService service = new CloseableExecutorService(executorService);
            List<IFuture<object>> futures = new List<IFuture<object>>();
            CountdownEvent startLatch = new CountdownEvent(QTY);
            for ( int i = 0; i<QTY; ++i )
            {
                IFuture<object> future = submitJoinRunnable(service, startLatch);
                futures.Add(future);
            }
            Assert.True(startLatch.Wait(TimeSpan.FromSeconds(15)));

            foreach ( IFuture<object> future in futures )
            {
                future.cancel();
            }
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Assert.AreEqual(0, service.size());
        }

        [Test]
        public void testListeningCallable()
        {
            CloseableExecutorService service = new CloseableExecutorService(executorService);
            CountdownEvent startLatch = new CountdownEvent(QTY);
            List<IFuture<object>> futures = new List<IFuture<object>>();
            for ( int i = 0; i<QTY; ++i )
            {
                IFuture<object> future = submitJoinRunnable(service, startLatch);
                futures.Add(future);
            }

            Assert.True(startLatch.Wait(TimeSpan.FromSeconds(15)));
            foreach ( IFuture<object> future in futures )
            {
                future.cancel();
            }
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Assert.AreEqual(0, service.size());
        }

        [Test]
        public void testPartialRunnable()
        {
            CountdownEvent outsideLatch = new CountdownEvent(1);
            executorService.submit
            (
                new FutureTask<object>(CallableUtils.FromFunc<object>(() =>
                {
                    try
                    {
                        Thread.CurrentThread.Join();
                    }
                    finally
                    {
                        outsideLatch.Signal();
                    }
                    return null;
                }))
            );

            CloseableExecutorService service = new CloseableExecutorService(executorService);
            CountdownEvent startLatch = new CountdownEvent(QTY);
            CountdownEvent latch = new CountdownEvent(QTY);
            for ( int i = 0; i<QTY; ++i )
            {
                submitRunnable(service, startLatch, latch);
            }

            while ( service.size() < QTY )
            {
                Thread.Sleep(100);
            }

            Assert.True(startLatch.Wait(TimeSpan.FromSeconds(15)));
            service.Dispose();
            Assert.True(latch.Wait(TimeSpan.FromSeconds(15)));
            Assert.AreEqual(1, outsideLatch.CurrentCount);
        }

        private void submitRunnable(CloseableExecutorService service, 
                                        CountdownEvent startLatch, 
                                        CountdownEvent latch)
        {
            CancellationTokenSource token = new CancellationTokenSource();
            service.submit(CallableUtils.FromFunc<object>(() =>
            {
                try
                {
                    startLatch.Signal();
                    Console.WriteLine(startLatch.CurrentCount);
                    int sleepTime = 100000;
                    while (!token.Token.IsCancellationRequested && sleepTime >= 0)
                    {
                        sleepTime -= 100;
                        Thread.Sleep(100);
                    }
                    if (token.Token.IsCancellationRequested)
                    {
                        Console.WriteLine("Stopped by cancel request");
                    }
                }
                finally
                {
                    latch.Signal();
                }
                return null;
            }), token);
        }

        private void submitJoinRunnable(CloseableExecutorService service,
                                CountdownEvent startLatch,
                                CountdownEvent latch)
        {
            CancellationTokenSource token = new CancellationTokenSource();
            service.submit(CallableUtils.FromFunc<object>(() =>
            {
                try
                {
                    startLatch.Signal();
                    Console.WriteLine(startLatch.CurrentCount);
                    int sleepTime = 100000;
                    while (!token.Token.IsCancellationRequested && sleepTime >= 0)
                    {
                        sleepTime -= 100;
                        if (Thread.CurrentThread.Join(100))
                        {
                            break;
                        }
                    }
                    if (token.Token.IsCancellationRequested)
                    {
                        Console.WriteLine("Stopped by cancel request");
                    }
                }
                finally
                {
                    latch.Signal();
                }
                return null;
            }), token);
        }

        private IFuture<object> submitJoinRunnable(CloseableExecutorService service,
                        CountdownEvent startLatch)
        {
            CancellationTokenSource token = new CancellationTokenSource();
            return service.submit(CallableUtils.FromFunc<object>(() =>
            {
                startLatch.Signal();
                Console.WriteLine(startLatch.CurrentCount);
                Thread.CurrentThread.Join();
                return null;
            }), token);
        }
    }
}
