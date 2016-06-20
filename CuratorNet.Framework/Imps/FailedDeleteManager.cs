using System;
using NLog;
using Org.Apache.CuratorNet.Client.Utils;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    class FailedDeleteManager
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly CuratorFramework client;
    
        internal volatile FailedDeleteManagerListener debugListener = null;

        internal interface FailedDeleteManagerListener
        {
            void pathAddedForDelete(String path);
        }

        internal FailedDeleteManager(CuratorFramework client)
        {
            this.client = client;
        }

        internal void addFailedDelete(String path)
        {
            if (debugListener != null)
            {
                debugListener.pathAddedForDelete(path);
            }

            if (client.getState() == CuratorFrameworkState.STARTED)
            {
                log.Debug("Path being added to guaranteed delete set: " + path);
                try
                {
                    client.delete().guaranteed().inBackground().forPath(path);
                }
                catch (Exception e)
                {
                    ThreadUtils.checkInterrupted(e);
                    addFailedDelete(path);
                }
            }
        }
    }
}