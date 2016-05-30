using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.Java.Types.Concurrent
{
    public interface IScheduledExecutorService : IExecutorService
    {
        IFuture<object> schedule(FutureTask<object> command, int delayMs);

        IFuture<object> scheduleWithFixedDelay(FutureTask<object> command,
                                                    int initialDelayMs,
                                                    int delayMs);
    }
}
