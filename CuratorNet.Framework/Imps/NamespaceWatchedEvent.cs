using org.apache.zookeeper;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class NamespaceWatchedEvent : WatchedEvent
    {
        NamespaceWatchedEvent(CuratorFrameworkImpl client, WatchedEvent @event) 
            : base(@event.getType(), @event.getState(), client.unfixForNamespace(@event.getPath()))
        { }
    }
}