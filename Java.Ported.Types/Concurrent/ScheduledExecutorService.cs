using System.Threading;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.Java.Types.Concurrent
{
    public class ScheduledExecutorService : TaskExecutorService, IScheduledExecutorService
    {
        /// <summary>
        /// Creates and executes a one-shot action that becomes enabled after the given delay.
        /// </summary>
        /// <param name="command">the task to execute</param>
        /// <param name="delayMs">the time from now to delay execution</param>
        /// <returns>a Future representing pending completion of the task</returns>
        public IFuture<object> schedule(FutureTask<object> command, int delayMs)
        {
            TaskFactory.StartNew(() =>
            {
                Thread.Sleep(delayMs);
                command.run();
            });
            return command;
        }

        /// <summary>
        /// Creates and executes a periodic action that becomes enabled first
        /// after the given initial delay, and subsequently with the
        /// given delay between the termination of one execution and the
        /// commencement of the next.  If any execution of the task
        /// encounters an exception, subsequent executions are suppressed.
        /// Otherwise, the task will only terminate via cancellation or
        /// termination of the executor.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command">the task to execute</param>
        /// <param name="initialDelayMs">the time to delay first execution</param>
        /// <param name="delayMs">the delay between the termination of one</param>
        /// <returns></returns>
        public IFuture<object> scheduleWithFixedDelay(FutureTask<object> command,
                                                        int initialDelayMs,
                                                        int delayMs)
        {
            TaskFactory.StartNew(() =>
            {
                Thread.Sleep(initialDelayMs);
                while (!command.CancelToken.IsCancellationRequested)
                {
                    Thread.Sleep(delayMs);
                    command.run();
                }
                command.cancel();
            });
            return command;
        }
    }
}
