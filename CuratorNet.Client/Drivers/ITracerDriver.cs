using System;

namespace Org.Apache.CuratorNet.Client.Drivers
{
    interface ITracerDriver
    {
        void addTrace(String name, long time, TimeUnit unit);

        /**
         * Add to a named counter
         *
         * @param name name of the counter
         * @param increment amount to increment
         */
        void addCount(String name, int increment);
    }
}
