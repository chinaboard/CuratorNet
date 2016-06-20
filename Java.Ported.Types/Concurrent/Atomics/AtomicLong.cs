using System.Threading;

namespace Org.Apache.Java.Types.Concurrent.Atomics
{
    public sealed class AtomicLong
    {
        private long _value;

        public AtomicLong() : this(0) { }

        public AtomicLong(long initialValue)
        {
            _value = initialValue;
        }

        /// <summary>
        /// Atomically adds the given value to the current value.
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        public long AddAndGet(long delta)
        {
            return Interlocked.Add(ref _value, delta);
        }

        /// <summary>
        /// Atomically sets the value to the given updated value if the current value == the expected value
        /// </summary>
        /// <param name="expect">Expected value</param>
        /// <param name="update">New value</param>
        /// <returns>Previous value</returns>
        public long CompareAndSet(long expect, long update)
        {
            return Interlocked.CompareExchange(ref _value, update, expect);
        }

        public long IncrementAndGet()
        {
            return Interlocked.Increment(ref _value);
        }

        public long GetAndIncrement()
        {
            long oldValue = _value;
            Interlocked.Increment(ref _value);
            return oldValue;
        }

        public long DecrementAndGet()
        {
            return Interlocked.Decrement(ref _value);
        }

        public long Get()
        {
            return Volatile.Read(ref _value);
        }

        public void Set(long value)
        {
            Volatile.Write(ref _value, value);
        }
    }
}
