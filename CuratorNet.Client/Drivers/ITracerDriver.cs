using System;

namespace Org.Apache.CuratorNet.Client.Drivers
{
    public interface ITracerDriver
    {
        void addTrace(string name, long timeMs);

        /**
         * Add to a named counter
         *
         * @param name name of the counter
         * @param increment amount to increment
         */
        void addCount(String name, int increment);
    }
}
