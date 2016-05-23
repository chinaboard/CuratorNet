using System.Threading;

namespace Org.Apache.Java.Types.Concurrent.Atomics
{
    public sealed class AtomicInteger
    {
        private volatile int _value;

        public AtomicInteger() : this(0) { }

        public AtomicInteger(int initialValue)
        {
            _value = initialValue;
        }

        /// <summary>
        /// Atomically adds the given value to the current value.
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        public int AddAndGet(int delta)
        {
            return Interlocked.Add(ref _value, delta);
        }

        /// <summary>
        /// Atomically sets the value to the given updated value if the current value == the expected value
        /// </summary>
        /// <param name="expect">Expected value</param>
        /// <param name="update">New value</param>
        /// <returns>Previous value</returns>
        public int CompareAndSet(int expect, int update)
        {
            return Interlocked.CompareExchange(ref _value, update, expect);
        }

        public int Get()
        {
            return _value;
        }

        public void Set(int value)
        {
            _value = value;
        }
    }
}
