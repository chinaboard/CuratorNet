using System;

namespace Org.Apache.CuratorNet.Client.Retry
{
    /**
     * Retry policy that retries a set number of times with an increasing (up to a maximum bound) sleep time between retries
     */
    public class BoundedExponentialBackoffRetry : ExponentialBackoffRetry
    {
        private readonly int maxSleepTimeMs;

        /**
         * @param baseSleepTimeMs initial amount of time to wait between retries
         * @param maxSleepTimeMs maximum amount of time to wait between retries
         * @param maxRetries maximum number of times to retry
         */
        public BoundedExponentialBackoffRetry(int baseSleepTimeMs, int maxSleepTimeMs, int maxRetries)
            : base(baseSleepTimeMs, maxRetries)
        {
            this.maxSleepTimeMs = maxSleepTimeMs;
        }

        public int getMaxSleepTimeMs()
        {
            return maxSleepTimeMs;
        }

        protected override int getSleepTimeMs(int retryCount, long elapsedTimeMs)
        {
            return Math.Min(maxSleepTimeMs, base.getSleepTimeMs(retryCount, elapsedTimeMs));
        }
    }
}
