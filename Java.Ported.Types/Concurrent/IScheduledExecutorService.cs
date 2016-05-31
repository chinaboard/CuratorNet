using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.Java.Types.Concurrent
{
    public interface IScheduledExecutorService : IExecutorService
    {
        IFuture<object> schedule(IRunnable command, int delayMs);

        IFuture<object> scheduleWithFixedDelay(IRunnable command,
                                                    int initialDelayMs,
                                                    int delayMs);
    }
}
