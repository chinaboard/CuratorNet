using System.Threading;

namespace Org.Apache.Java.Types.Concurrent.Atomics
{
    public sealed class AtomicReference<T> where T : class 
    {
        private volatile T _value;

        public AtomicReference() : this(null) { }

        public AtomicReference(T initialValue)
        {
            _value = initialValue;
        }

        /// <summary>
        /// Atomically sets the value to the given updated value if the current value == the expected value
        /// </summary>
        /// <param name="expect">Expected value</param>
        /// <param name="update">New value</param>
        /// <returns>Previous value</returns>
        public bool CompareAndSet(T expect, T update)
        {
            return Interlocked.CompareExchange(ref _value, update, expect) == expect;
        }
        
        public T Get()
        {
            return _value;
        }

        public void Set(T value)
        {
            _value = value;
        }
    }
}
