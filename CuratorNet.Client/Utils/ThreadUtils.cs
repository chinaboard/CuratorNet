using System;
using System.Threading;
using NLog;
using Org.Apache.Java.Types;

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

        public static ExecutorService newSingleThreadExecutor(String processName)
        {
            return Executors.newSingleThreadExecutor(newThreadFactory(processName));
        }

        public static ExecutorService newFixedThreadPool(int qty, String processName)
        {
            return Executors.newFixedThreadPool(qty, newThreadFactory(processName));
        }

        public static ScheduledExecutorService newSingleThreadScheduledExecutor(String processName)
        {
            return Executors.newSingleThreadScheduledExecutor(newThreadFactory(processName));
        }

        public static ScheduledExecutorService newFixedThreadScheduledPool(int qty, String processName)
        {
            return Executors.newScheduledThreadPool(qty, newThreadFactory(processName));
        }

        public static ThreadFactory newThreadFactory(String processName)
        {
            return newGenericThreadFactory("Curator-" + processName);
        }

        public static ThreadFactory newGenericThreadFactory(String processName)
        {
            Thread.UncaughtExceptionHandler uncaughtExceptionHandler = new Thread.UncaughtExceptionHandler()
        {
            @Override
            public void uncaughtException(Thread t, Throwable e)
        {
            log.error("Unexpected exception in thread: " + t, e);
            Throwables.propagate(e);
        }
    };
        return new ThreadFactoryBuilder()
            .setNameFormat(processName + "-%d")
            .setDaemon(true)
            .setUncaughtExceptionHandler(uncaughtExceptionHandler)
            .build();
}

public static String getProcessName(Class<?> clazz)
{
    if (clazz.isAnonymousClass())
    {
        return getProcessName(clazz.getEnclosingClass());
    }
    return clazz.getSimpleName();
}
}
}
