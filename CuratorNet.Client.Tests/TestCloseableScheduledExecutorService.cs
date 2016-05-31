using System;
using System.Threading;
using NUnit.Framework;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.Java.Types;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Atomics;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace CuratorNet.Client.Tests
{
    public class TestCloseableScheduledExecutorService
    {
        private const int QTY = 10;
        private const int DELAY_MS = 100;

        private volatile IScheduledExecutorService executorService;

        [SetUp]
        public void setup()
        {
            executorService = ThreadUtils.newFixedThreadScheduledPool(QTY * 2, "testProcess");
        }

        [TearDown]
        public void tearDown()
        {
        }

        [Test]
        public void testCloseableScheduleWithFixedDelay()
        {
            CloseableScheduledExecutorService service 
                = new CloseableScheduledExecutorService(executorService);

            CountdownEvent latch = new CountdownEvent(1);
            service.scheduleWithFixedDelay(RunnableUtils.FromFunc(() => latch.Signal()),
                DELAY_MS,
                DELAY_MS
            );
            Assert.True(latch.Wait((QTY * 2) * DELAY_MS));
        }

        [Test]
        public void testCloseableScheduleWithFixedDelayAndAdditionalTasks()
        {
            AtomicInteger outerCounter = new AtomicInteger(0);
            IRunnable command = RunnableUtils.FromFunc(() => { Console.WriteLine("--");
                                                                 outerCounter.IncrementAndGet();
            });
            executorService.scheduleWithFixedDelay(command, DELAY_MS, DELAY_MS);
            CloseableScheduledExecutorService service = new CloseableScheduledExecutorService(executorService);
            AtomicInteger innerCounter = new AtomicInteger(0);
            service.scheduleWithFixedDelay(RunnableUtils.FromFunc(() => { Console.WriteLine("!!"); innerCounter.IncrementAndGet(); }),
                                            DELAY_MS, 
                                            DELAY_MS);

            Thread.Sleep(DELAY_MS * 4);

            service.Dispose();
            Thread.Sleep(DELAY_MS * 2);
            Assert.AreEqual(0, service.size());
            int innerValue = innerCounter.Get();
            Assert.True(innerValue > 0);

            int value = outerCounter.Get();
            Thread.Sleep(DELAY_MS* 2);
            int newValue = outerCounter.Get();
            Assert.True(newValue > value);
            Assert.AreEqual(innerValue, innerCounter.Get());

            value = newValue;
            Thread.Sleep(DELAY_MS* 2);
            newValue = outerCounter.Get();
            Assert.True(newValue > value);
            Assert.AreEqual(innerValue, innerCounter.Get());
        }
    }

}
