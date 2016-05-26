using System;
using System.Threading;
using NLog;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Drivers;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Atomics;

namespace Org.Apache.CuratorNet.Client
{
    /**
     * <p>Mechanism to perform an operation on Zookeeper that is safe against
     * disconnections and "recoverable" errors.</p>
     *
     * <p>
     * If an exception occurs during the operation, the RetryLoop will process it,
     * check with the current retry policy and either attempt to reconnect or re-throw
     * the exception
     * </p>
     *
     * Canonical usage:<br>
     * <pre>
     * RetryLoop retryLoop = client.newRetryLoop();
     * while ( retryLoop.shouldContinue() )
     * {
     *     try
     *     {
     *         // do your work
     *         ZooKeeper      zk = client.getZooKeeper();    // it's important to re-get the ZK instance in case there was an error and the instance was re-created
     *
     *         retryLoop.markComplete();
     *     }
     *     catch ( Exception e )
     *     {
     *         retryLoop.takeException(e);
     *     }
     * }
     * </pre>
     */
    public class RetryLoop
    {
        private bool isDone = false;
        private int retryCount = 0;

        private readonly Logger            log = LogManager.GetCurrentClassLogger();
        private readonly long startTimeMs = DateTime.Now.Ticks;
        private readonly IRetryPolicy       retryPolicy;
        private readonly AtomicReference<ITracerDriver>     tracer;

        private class ThreadSleeper : IRetrySleeper
        {
            public void sleepFor(int timeMs)
            {
                Thread.Sleep(timeMs);
            }
        }

        private static readonly IRetrySleeper sleeper = new ThreadSleeper();

        /**
         * Returns the default retry sleeper
         *
         * @return sleeper
         */
        public static IRetrySleeper getDefaultRetrySleeper()
        {
            return sleeper;
        }

        /**
         * Convenience utility: creates a retry loop calling the given proc and retrying if needed
         *
         * @param client Zookeeper
         * @param proc procedure to call with retry
         * @param <T> return type
         * @return procedure result
         * @throws Exception any non-retriable errors
         */
        public static T callWithRetry<T>(CuratorZookeeperClient client, 
                                            ICallable<T> proc)
        {
            T result = default(T);
            RetryLoop retryLoop = client.newRetryLoop();
            while ( retryLoop.shouldContinue() )
            {
                try
                {
                    client.internalBlockUntilConnectedOrTimedOut();

                    result = proc.call();
                    retryLoop.markComplete();
                }
                catch (Exception e)
                {
                    ThreadUtils.checkInterrupted(e);
                    retryLoop.takeException(e);
                }
            }
            return result;
        }

        public RetryLoop(IRetryPolicy retryPolicy, AtomicReference<ITracerDriver> tracer)
        {
            this.retryPolicy = retryPolicy;
            this.tracer = tracer;
        }

        /**
         * If true is returned, make an attempt at the operation
         *
         * @return true/false
         */
        public bool shouldContinue()
        {
            return !isDone;
        }

        /**
            * Call this when your operation has successfully completed
            */
        public void markComplete()
        {
            isDone = true;
        }

        /**
         * Utility - return true if the given Zookeeper result code is retry-able
         *
         * @param rc result code
         * @return true/false
         */
        public static bool shouldRetry(KeeperException keeperException)
        {
            return (keeperException is KeeperException.ConnectionLossException) ||
                    (keeperException is KeeperException.OperationTimeoutException) ||
                    (keeperException is KeeperException.SessionMovedException) ||
                    (keeperException is KeeperException.SessionExpiredException);
        }

        /**
         * Utility - return true if the given exception is retry-able
         *
         * @param exception exception to check
         * @return true/false
         */
        public static bool isRetryException(Exception exception)
        {
            if (exception is KeeperException )
            {
                KeeperException keeperException = (KeeperException)exception;
                return shouldRetry(keeperException);
            }
            return false;
        }

        /**
         * Pass any caught exceptions here
         *
         * @param exception the exception
         * @throws Exception if not retry-able or the retry policy returned negative
         */
        public void takeException(Exception exception)
        {
            bool rethrow = true;
            if ( isRetryException(exception) )
            {
                log.Debug(exception, "Retry-able exception received");
                long elapsedTimeMs = (DateTime.Now.Ticks - startTimeMs)/1000;
                if (retryPolicy.allowRetry(retryCount++, elapsedTimeMs, sleeper))
                {
                    tracer.Get().addCount("retries-allowed", 1);
                    log.Debug("Retrying operation");
                    rethrow = false;
                }
                else
                {
                    tracer.Get().addCount("retries-disallowed", 1);
                    log.Debug("Retry policy not allowing retry");
                }
            }

            if ( rethrow )
            {
                throw exception;
            }
        }
    }
}
