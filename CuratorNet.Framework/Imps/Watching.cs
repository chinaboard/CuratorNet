using org.apache.zookeeper;
using Org.Apache.CuratorNet.Framework.API;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class Watching
    {
        private readonly Watcher       watcher;
        private readonly bool       watched;

        Watching(bool watched)
        {
            this.watcher = null;
            this.watched = watched;
        }

        Watching(CuratorFrameworkImpl client, Watcher watcher)
        {
            this.watcher = (watcher != null) ? client.getNamespaceWatcherMap().getNamespaceWatcher(watcher) : null;
            this.watched = false;
        }

        Watching(CuratorFrameworkImpl client, CuratorWatcher watcher)
        {
            this.watcher = (watcher != null) ? client.getNamespaceWatcherMap().getNamespaceWatcher(watcher) : null;
            this.watched = false;
        }

        Watching()
        {
            watcher = null;
            watched = false;
        }

        Watcher getWatcher()
        {
            return watcher;
        }

        bool isWatched()
        {
            return watched;
        }
    }
}