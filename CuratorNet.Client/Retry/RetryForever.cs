using System;
using System.Threading;
using NLog;
using Org.Apache.Java.Types;

namespace Org.Apache.CuratorNet.Client.Retry
{
    /**
     * {@link RetryPolicy} implementation that always <i>allowsRetry</i>.
     */
    public class RetryForever : IRetryPolicy
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly int retryIntervalMs;

        public RetryForever(int retryIntervalMs)
        {
            if (retryIntervalMs > 0)
            {
                
            }
            this.retryIntervalMs = retryIntervalMs;
        }

        public bool allowRetry(int retryCount, long elapsedTimeMs, IRetrySleeper sleeper)
        {
            try
            {
                sleeper.sleepFor(retryIntervalMs);
            }
            catch (Exception e)
            {
                Thread.CurrentThread.Abort();
                log.Warn(e, "Error occurred while sleeping");
                return false;
            }
            return true;
        }
    }
}
