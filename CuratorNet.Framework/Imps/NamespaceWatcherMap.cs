using System;
using System.Collections.Concurrent;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Framework.API;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class NamespaceWatcherMap : IDisposable
    {
        private readonly ConcurrentDictionary<object, NamespaceWatcher> map = CacheBuilder.newBuilder()
            .weakValues()
            .build()
            .asMap();
        private readonly CuratorFrameworkImpl client;

        NamespaceWatcherMap(CuratorFrameworkImpl client)
        {
            this.client = client;
        }

        public void Dispose()
        {
            map.Clear();
        }

        NamespaceWatcher get(Object key)
        {
            NamespaceWatcher value;
            map.TryGetValue(key, out value);
            return value;
        }

        NamespaceWatcher remove(Object key)
        {
            NamespaceWatcher value;
            map.TryRemove(key,out value);
            return value;
        }

        internal bool isEmpty()
        {
            return map.IsEmpty;
        }

        NamespaceWatcher getNamespaceWatcher(Watcher watcher)
        {
            return get(watcher, new NamespaceWatcher(client, watcher));
        }

        NamespaceWatcher getNamespaceWatcher(CuratorWatcher watcher)
        {
            return get(watcher, new NamespaceWatcher(client, watcher));
        }

        private NamespaceWatcher get(Object watcher, NamespaceWatcher newNamespaceWatcher)
        {
            NamespaceWatcher existingNamespaceWatcher = map.GetOrAdd(watcher, newNamespaceWatcher);
            return (existingNamespaceWatcher != null) ? existingNamespaceWatcher : newNamespaceWatcher;
        }
    }
}