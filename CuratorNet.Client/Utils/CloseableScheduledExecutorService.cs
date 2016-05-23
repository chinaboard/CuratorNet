using System;
using Org.Apache.Java.Types;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Futures;

namespace Org.Apache.CuratorNet.Client.Utils
{
    /// <summary>
    /// Decoration on an ScheduledExecutorService that tracks created futures 
    /// and provides a method to close futures created via this class
    /// </summary>
    public class CloseableScheduledExecutorService : CloseableExecutorService
    {
        private readonly ScheduledExecutorService _scheduledExecService;

        /**
         * @param scheduledExecutorService the service to decorate
         */
        public CloseableScheduledExecutorService(ScheduledExecutorService scheduledExecService)
            : base(scheduledExecService)
        {
            _scheduledExecService = scheduledExecService;
        }

        /**
         * Creates and executes a one-shot action that becomes enabled
         * after the given delay.
         *
         * @param task  the task to execute
         * @param delay the time from now to delay execution
         * @param unit  the time unit of the delay parameter
         * @return a Future representing pending completion of
         *         the task and whose <tt>get()</tt> method will return
         *         <tt>null</tt> upon completion
         */
        public IFuture<object> schedule(IRunnable task, int delayMs)
        {
            if (!isOpen.get())
            {
                throw new InvalidOperationException("CloseableExecutorService is closed");
            }

            InternalFutureTask<object> futureTask 
                = new InternalFutureTask<object>(this, new FutureTask<object>(task));
            _scheduledExecService.schedule(futureTask, delayMs);
            return futureTask;
        }

        /**
         * Creates and executes a periodic action that becomes enabled first
         * after the given initial delay, and subsequently with the
         * given delay between the termination of one execution and the
         * commencement of the next.  If any execution of the task
         * encounters an exception, subsequent executions are suppressed.
         * Otherwise, the task will only terminate via cancellation or
         * termination of the executor.
         *
         * @param task      the task to execute
         * @param initialDelay the time to delay first execution
         * @param delay        the delay between the termination of one
         *                     execution and the commencement of the next
         * @param unit         the time unit of the initialDelay and delay parameters
         * @return a Future representing pending completion of
         *         the task, and whose <tt>get()</tt> method will throw an
         *         exception upon cancellation
         */
        public IFuture<object> scheduleWithFixedDelay(IRunnable task, 
                                                    int initialDelay, 
                                                    int delayMs)
        {
            if (!isOpen.get())
            {
                throw new InvalidOperationException("CloseableExecutorService is closed");
            }
            InternalFutureTask<object> futureTask
                = new InternalFutureTask<object>(this, new FutureTask<object>(task));
            IFuture<object> scheduledFuture 
                = _scheduledExecService.scheduleWithFixedDelay(futureTask, initialDelay, delayMs);
            return new InternalScheduledFutureTask(this, scheduledFuture);
        }
    }
}
