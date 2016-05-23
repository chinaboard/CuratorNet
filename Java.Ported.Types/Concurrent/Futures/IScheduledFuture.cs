namespace Org.Apache.Java.Types.Concurrent.Futures
{
    public interface IScheduledFuture<T> : IFuture<T>
    {
        /**
         * Returns the remaining delay associated with this object, in the
         * given time unit.
         *
         * @param unit the time unit
         * @return the remaining delay; zero or negative values indicate
         * that the delay has already elapsed
         */
        long getDelayMs();
    }
}
