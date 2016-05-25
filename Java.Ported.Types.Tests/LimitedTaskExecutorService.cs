using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Atomics;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace Java.Ported.Types.Tests
{
    [TestFixture]
    public class LimitedTaskExecutorServiceTest
    {
        [Test]
        [TestCase(20, 80)]
        [TestCase(40, 120)]
        [TestCase(80, 240)]
        [TestCase(120, 320)]
        [TestCase(350, 1024)]
        [TestCase(20, 1024)]
        public void Setup(int maxTasksInPool, int tasksToFork)
        {
            AtomicInteger atomicVal = new AtomicInteger();
            IExecutorService execService = new LimitedTaskExecutorService(maxTasksInPool);
            ICollection<IFuture<int>> futures = new List<IFuture<int>>(tasksToFork);
            for (int i = 0; i < tasksToFork; i++)
            {
                var task = new FutureTask<int>(CallableUtils.FromFunc(() =>
                {
                    Thread.Yield();
                    Thread.Sleep(7);
                    int value = atomicVal.IncrementAndGet();
                    Thread.Yield();
                    return value;
                }));
                IFuture<int> future = execService.submit(task);
                futures.Add(future);
            }
            var results = new List<int>();
            foreach (IFuture<int> future in futures)
            {
                int value = future.get();
                results.Add(value);
            }
            results.Sort();
            int prevValue = results[0];
            Console.WriteLine(prevValue);
            for (int i = 1; i < results.Count; i++)
            {
                Console.WriteLine(results[i]);
                Assert.AreEqual(prevValue + 1, results[i]);
                prevValue = results[i];
            }
            Assert.AreEqual(atomicVal.Get(), tasksToFork);
        }
    }
}
