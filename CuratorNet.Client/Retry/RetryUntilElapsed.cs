using System;

namespace Org.Apache.CuratorNet.Client.Retry
{
    /**
     * A retry policy that retries until a given amount of time elapses
     */
    public class RetryUntilElapsed : SleepingRetry
    {
        private readonly int maxElapsedTimeMs;
        private readonly int sleepMsBetweenRetries;

        public RetryUntilElapsed(int maxElapsedTimeMs, int sleepMsBetweenRetries) : base(Int32.MaxValue)
        {
            this.maxElapsedTimeMs = maxElapsedTimeMs;
            this.sleepMsBetweenRetries = sleepMsBetweenRetries;
        }

        public override bool allowRetry(int retryCount, long elapsedTimeMs, IRetrySleeper sleeper)
        {
            return base.allowRetry(retryCount, elapsedTimeMs, sleeper) 
                    && (elapsedTimeMs < maxElapsedTimeMs);
        }

        protected override int getSleepTimeMs(int retryCount, long elapsedTimeMs)
        {
            return sleepMsBetweenRetries;
        }
    }
}
