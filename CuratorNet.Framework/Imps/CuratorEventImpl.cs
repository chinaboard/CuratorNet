using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Framework.API;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class CuratorEventImpl : ICuratorEvent
    {
        private readonly CuratorEventType  type;
        private readonly int resultCode;
        private readonly String            path;
        private readonly String            name;
        private readonly List<String>      children;
        private readonly Object            context;
        private readonly Stat              stat;
        private readonly byte[] data;
        private readonly WatchedEvent      watchedEvent;
        private readonly ICollection<ACL>         aclList;

        public CuratorEventType getType()
        {
            return type;
        }

        public int getResultCode()
        {
            return resultCode;
        }

        public String getPath()
        {
            return path;
        }

        public Object getContext()
        {
            return context;
        }

        public Stat getStat()
        {
            return stat;
        }

        public byte[] getData()
        {
            return data;
        }

        public String getName()
        {
            return name;
        }

        public List<String> getChildren()
        {
            return children;
        }

        public WatchedEvent getWatchedEvent()
        {
            return watchedEvent;
        }

        public List<ACL> getACLList()
        {
            return new List<ACL>(aclList);
        }

        internal CuratorEventImpl(CuratorFrameworkImpl client, 
                            CuratorEventType type, 
                            int resultCode, 
                            String path, 
                            String name, 
                            Object context, 
                            Stat stat, 
                            byte[] data, 
                            List<String> children, 
                            WatchedEvent watchedEvent, 
                            List<ACL> aclList)
        {
            this.type = type;
            this.resultCode = resultCode;
            this.path = client.unfixForNamespace(path);
            this.name = name;
            this.context = context;
            this.stat = stat;
            this.data = data;
            this.children = children;
            this.watchedEvent = (watchedEvent != null) 
                                    ? new NamespaceWatchedEvent(client, watchedEvent) 
                                    : watchedEvent;
            this.aclList = (aclList != null) 
                                ? new ReadOnlyCollectionBuilder<ACL>(aclList).ToReadOnlyCollection() 
                                : null;
        }

        public String toString()
        {
            return "CuratorEventImpl{" +
                   "type=" + type +
                   ", resultCode=" + resultCode +
                   ", path='" + path + '\'' +
                   ", name='" + name + '\'' +
                   ", children=" + children +
                   ", context=" + context +
                   ", stat=" + stat +
                   ", data=" + BitConverter.ToString(data) +
                   ", watchedEvent=" + watchedEvent +
                   ", aclList=" + aclList +
                   '}';
        }
    }
}
