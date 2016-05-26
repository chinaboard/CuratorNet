using System.Threading;

namespace Org.Apache.Java.Types.Concurrent.Atomics
{
    public class AtomicBoolean
    {
        private volatile int _value;

        public AtomicBoolean(bool initialValue)
        {
            int intVal = initialValue ? 1 : 0;
            Interlocked.Exchange(ref _value, intVal);
        }

        public bool get()
        {
            return _value == 1;
        }

        public bool getAndSet(bool value)
        {
            int intVal = value ? 1 : 0;
            return Interlocked.Exchange(ref _value, intVal) == 1;
        }

        public void set(bool value)
        {
            int intVal = value ? 1 : 0;
            Interlocked.Exchange(ref _value, intVal);
        }

        /// <summary>
        /// Atomically sets the value to the given updated value if the current value == the expected value.
        /// </summary>
        /// <param name="expect"></param>
        /// <param name="update"></param>
        /// <returns>true if successful. False return indicates that the actual value was not equal to the expected value.</returns>
        public bool compareAndSet(bool expect, bool update)
        {
            int expectedInt = expect ? 1 : 0;
            int updateInt = update ? 1 : 0;
            int origValue = Interlocked.CompareExchange(ref _value, updateInt, expectedInt);
            return origValue == expectedInt;
        }
    }
}
