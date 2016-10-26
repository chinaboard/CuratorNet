using org.apache.zookeeper;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class NamespaceWatchedEvent : WatchedEvent
    {
        internal NamespaceWatchedEvent(CuratorFrameworkImpl client, WatchedEvent @event) 
            : base(@event.get_Type(), @event.getState(), client.unfixForNamespace(@event.getPath()))
        { }
    }
}