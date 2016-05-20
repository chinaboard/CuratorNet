namespace Org.Apache.CuratorNet.Client.Retry
{
    /**
     * A retry policy that retries only once
     */
    public class RetryOneTime : RetryNTimes
    {
        public RetryOneTime(int sleepMsBetweenRetry) : base(1, sleepMsBetweenRetry)
        {
        }
    }
}
