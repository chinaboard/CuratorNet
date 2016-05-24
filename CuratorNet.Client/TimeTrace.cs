using System;
using Org.Apache.CuratorNet.Client.Drivers;

namespace Org.Apache.CuratorNet.Client
{
    /**
     * Utility to time a method or portion of code
     */
    public class TimeTrace
    {
        private readonly string name;
        private readonly ITracerDriver driver;
        private readonly long startTimeNanos = DateTime.Now.Ticks;

        /**
         * Create and start a timer
         *
         * @param name name of the event
         * @param driver driver
         */
        public TimeTrace(String name, ITracerDriver driver)
        {
            this.name = name;
            this.driver = driver;
        }

        /**
         * Record the elapsed time
         */
        public void commit()
        {
            long elapsed = DateTime.Now.Ticks - startTimeNanos;
            driver.addTrace(name, elapsed / 1000);
        }
    }
}
