using System;
using NLog;
using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.CuratorNet.Client.Utils
{
    public class ThreadUtils
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public static void checkInterrupted(Exception e)
        {
//            if (e is InterruptedException )
//            {
//                Thread.currentThread().interrupt();
//            }
        }

        public static IExecutorService newSingleThreadExecutor(String processName)
        {
            return new LimitedTaskExecutorService(1);
        }

        public static IExecutorService newFixedThreadPool(int qty, String processName)
        {
            return new LimitedTaskExecutorService(qty);
        }

        public static IScheduledExecutorService newSingleThreadScheduledExecutor(String processName)
        {
            return new LimitedTaskExecutorService(1);
        }

        public static IScheduledExecutorService newFixedThreadScheduledPool(int qty, String processName)
        {
            return new LimitedTaskExecutorService(qty);
        }

        public static String getProcessName(Type clazz)
        {
            return clazz.FullName;
        }
    }
}
