namespace Org.Apache.CuratorNet.Client.Retry
{
    /**
     * Retry policy that retries a max number of times
     */
    public class RetryNTimes : SleepingRetry
    {
        private readonly int sleepMsBetweenRetries;

        public RetryNTimes(int n, int sleepMsBetweenRetries) : base(n)
        {
            this.sleepMsBetweenRetries = sleepMsBetweenRetries;
        }

        protected override int getSleepTimeMs(int retryCount, long elapsedTimeMs)
        {
            return sleepMsBetweenRetries;
        }
    }
}
