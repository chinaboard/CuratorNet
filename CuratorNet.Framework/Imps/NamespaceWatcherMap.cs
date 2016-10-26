using System;
using System.Collections.Concurrent;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Framework.API;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class NamespaceWatcherMap : IDisposable
    {
        private readonly ConcurrentDictionary<object, WeakReference<NamespaceWatcher>> map 
            = new ConcurrentDictionary<object, WeakReference<NamespaceWatcher>>();
        private readonly CuratorFrameworkImpl client;

        internal NamespaceWatcherMap(CuratorFrameworkImpl client)
        {
            this.client = client;
        }

        public void Dispose()
        {
            map.Clear();
        }

        internal NamespaceWatcher get(Object key)
        {
            WeakReference<NamespaceWatcher> weakValue;
            map.TryGetValue(key, out weakValue);
            NamespaceWatcher value;
            weakValue.TryGetTarget(out value);
            return value;
        }

        internal NamespaceWatcher remove(Object key)
        {
            WeakReference<NamespaceWatcher> weakValue;
            map.TryRemove(key,out weakValue);
            NamespaceWatcher value;
            weakValue.TryGetTarget(out value);
            return value;
        }

        internal bool isEmpty()
        {
            return map.IsEmpty;
        }

        internal NamespaceWatcher getNamespaceWatcher(Watcher watcher)
        {
            return get(watcher, new NamespaceWatcher(client, watcher));
        }

        internal NamespaceWatcher getNamespaceWatcher(CuratorWatcher watcher)
        {
            return get(watcher, new NamespaceWatcher(client, watcher));
        }

        private NamespaceWatcher get(Object watcher, NamespaceWatcher newNamespaceWatcher)
        {
            var weakReference = new WeakReference<NamespaceWatcher>(newNamespaceWatcher);
            WeakReference<NamespaceWatcher> weakExistingValue = map.GetOrAdd(watcher, weakReference);
            NamespaceWatcher existingNamespaceWatcher;
            weakExistingValue.TryGetTarget(out existingNamespaceWatcher);
            return existingNamespaceWatcher ?? newNamespaceWatcher;
        }
    }
}