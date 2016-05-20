using System;
using System.Threading;
using Org.Apache.Java.Types;

namespace Org.Apache.CuratorNet.Client.Retry
{
    public abstract class SleepingRetry : IRetryPolicy
    {
        private readonly int n;

        protected SleepingRetry(int n)
        {
            this.n = n;
        }

        /// <summary>
        /// Made public for testing
        /// </summary>
        /// <returns></returns>
        public int getN()
        {
            return n;
        }

        public virtual bool allowRetry(int retryCount, long elapsedTimeMs, IRetrySleeper sleeper)
        {
            if (retryCount < n)
            {
                try
                {
                    sleeper.sleepFor(getSleepTimeMs(retryCount, elapsedTimeMs), TimeUnit.Milliseconds);
                }
                catch (Exception)
                {
                    Thread.CurrentThread.Abort();
                    return false;
                }
                return true;
            }
            return false;
        }

        protected abstract int getSleepTimeMs(int retryCount, long elapsedTimeMs);
    }
}
