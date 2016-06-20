using System;

namespace Org.Apache.CuratorNet.Client.Utils
{
    public static class DateTimeUtils
    {
        /// <summary>
        /// Get current time in milliseconds
        /// </summary>
        /// <returns></returns>
        public static long GetCurrentMs()
        {
            return DateTime.Now.Ticks / 1000;
        }
    }
}
