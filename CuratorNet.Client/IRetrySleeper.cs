namespace Org.Apache.CuratorNet.Client
{
    /**
     * Abstraction for retry policies to sleep
     */
    public interface IRetrySleeper
    {
        /**
         * Sleep for the given time
         *
         * @param time time
         * @param unit time unit
         * @throws InterruptedException if the sleep is interrupted
         */
        void sleepFor(long time, TimeUnit unit);
    }
}
