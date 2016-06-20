using System;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Client.Utils;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.Java.Types;
using Org.Apache.Java.Types.Concurrent.Atomics;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class OperationAndData<T> : Delayed, IRetrySleeper
    {
        private static readonly AtomicLong nextOrdinal = new AtomicLong();

        private readonly IBackgroundOperation<T> operation;
        private readonly T data;
        private readonly IBackgroundCallback callback;
        private readonly long startTimeMs = GetCurrentTimeMs();

        private readonly ErrorCallback<T> errorCallback;

        private readonly AtomicInteger retryCount = new AtomicInteger(0);

        private readonly AtomicLong sleepUntilTimeMs = new AtomicLong(0);

        private readonly AtomicLong ordinal = new AtomicLong();

        private readonly Object context;

        internal interface ErrorCallback<T>
        {
            void retriesExhausted(OperationAndData<T> operationAndData);
        }

        internal OperationAndData(IBackgroundOperation<T> operation, T data, IBackgroundCallback callback, ErrorCallback<T> errorCallback, Object context)
        {
            this.operation = operation;
            this.data = data;
            this.callback = callback;
            this.errorCallback = errorCallback;
            this.context = context;
            reset();
        }

        internal void reset()
        {
            retryCount.Set(0);
            ordinal.Set(nextOrdinal.GetAndIncrement());
        }

        internal Object getContext()
        {
            return context;
        }

        internal void callPerformBackgroundOperation()
        {
            operation.performBackgroundOperation(this);
        }

        internal T getData()
        {
            return data;
        }

        internal long getElapsedTimeMs()
        {
            return GetCurrentTimeMs() - startTimeMs;
        }

        internal int getThenIncrementRetryCount()
        {
            return retryCount.GetAndIncrement();
        }

        internal IBackgroundCallback getCallback()
        {
            return callback;
        }

        internal ErrorCallback<T> getErrorCallback()
        {
            return errorCallback;
        }

        internal IBackgroundOperation<T> getOperation()
        {
            return operation;
        }

        public void sleepFor(long timeMs)
        {
            sleepUntilTimeMs.Set(GetCurrentTimeMs() + timeMs);
        }

        public long getDelay()
        {
            return sleepUntilTimeMs.Get() - GetCurrentTimeMs();
        }

        public int compareTo(Delayed o)
        {
            if (o == this)
            {
                return 0;
            }

            long diff = getDelay() - o.getDelay();
            if (diff == 0)
            {
                if (o is OperationAndData )
                {
                    diff = ordinal.Get() - ((OperationAndData)o).ordinal.Get();
                }
            }

            return (diff < 0) ? -1 : ((diff > 0) ? 1 : 0);
        }

        private static long GetCurrentTimeMs()
        {
            return DateTimeUtils.GetCurrentMs();
        }
    }
}