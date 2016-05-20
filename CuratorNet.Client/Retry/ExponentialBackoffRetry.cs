using System;
using NLog;

namespace Org.Apache.CuratorNet.Client.Retry
{
    /**
     * Retry policy that retries a set number of times with increasing sleep time between retries
     */
    public class ExponentialBackoffRetry : SleepingRetry
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private const int MAX_RETRIES_LIMIT = 29;
        private const int DEFAULT_MAX_SLEEP_MS = Int32.MaxValue;

        private readonly Random random = new Random();
        private readonly int baseSleepTimeMs;
        private readonly int maxSleepMs;

        /**
         * @param baseSleepTimeMs initial amount of time to wait between retries
         * @param maxRetries max number of times to retry
         */
        public ExponentialBackoffRetry(int baseSleepTimeMs, int maxRetries) 
            : this(baseSleepTimeMs, maxRetries, DEFAULT_MAX_SLEEP_MS) {}

        /**
         * @param baseSleepTimeMs initial amount of time to wait between retries
         * @param maxRetries max number of times to retry
         * @param maxSleepMs max time in ms to sleep on each retry
         */
        public ExponentialBackoffRetry(int baseSleepTimeMs, int maxRetries, int maxSleepMs) 
            : base(validateMaxRetries(maxRetries))
        {
            this.baseSleepTimeMs = baseSleepTimeMs;
            this.maxSleepMs = maxSleepMs;
        }

        public int getBaseSleepTimeMs()
        {
            return baseSleepTimeMs;
        }

        protected override int getSleepTimeMs(int retryCount, long elapsedTimeMs)
        {
            // copied from Hadoop's RetryPolicies.java
            int sleepMs = baseSleepTimeMs * Math.Max(1, random.Next(1 << (retryCount + 1)));
            if (sleepMs > maxSleepMs)
            {
                log.Warn("Sleep extension too large ({0}). Pinning to {1}", 
                            sleepMs, maxSleepMs);
                sleepMs = maxSleepMs;
            }
            return sleepMs;
        }

        private static int validateMaxRetries(int maxRetries)
        {
            if (maxRetries > MAX_RETRIES_LIMIT)
            {
                log.Warn("maxRetries too large ({0}). Pinning to {1}", 
                            maxRetries, MAX_RETRIES_LIMIT);
                maxRetries = MAX_RETRIES_LIMIT;
            }
            return maxRetries;
        }
    }
}
