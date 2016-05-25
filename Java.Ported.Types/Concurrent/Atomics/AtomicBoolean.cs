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

        public bool compareAndSet(bool expect, bool update)
        {
            int expectedInt = expect ? 1 : 0;
            int updateInt = update ? 1 : 0;
            int origValue = Interlocked.CompareExchange(ref _value, updateInt, expectedInt);
            return origValue == 1;
        }
    }
}
