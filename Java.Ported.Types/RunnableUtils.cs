using System;
using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.Java.Types
{
    public static class RunnableUtils
    {
        internal class RunnableFromFunc : IRunnable
        {
            private readonly Action _func;

            /// <summary>
            /// Initializes a new instance of the <see cref="T:System.Object"/> class.
            /// </summary>
            public RunnableFromFunc(Action func)
            {
                if (func == null)
                {
                    throw new ArgumentNullException(nameof(func));
                }
                _func = func;
            }

            /// <summary>
            /// When an object implementing interface <code>Runnable</code> is used
            /// to create a thread, starting the thread causes the object's
            /// <code>run</code> method to be called in that separately executing
            /// thread.
            /// <p>
            /// The general contract of the method <code>run</code> is that it may
            /// take any action whatsoever.
            /// </summary>
            public void run()
            {
                _func();
            }
        }

        public static IRunnable FromFunc(Action func)
        {
            return new RunnableFromFunc(func);
        }
    }
}
