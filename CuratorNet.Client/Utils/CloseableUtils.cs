using System;
using NLog;

namespace Org.Apache.CuratorNet.Client.Utils
{
    public class CloseableUtils
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void closeQuietly(IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception e)
            {
                Log.Error(e, "IOException should not have been thrown.");
            }
        }
    }

}
