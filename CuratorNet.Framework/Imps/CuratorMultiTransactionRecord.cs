using System;
using System.Collections.Generic;
using org.apache.zookeeper;
using Org.Apache.CuratorNet.Framework.API.Transaction;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class CuratorMultiTransactionRecord : MultiTransactionRecord
    {
        private List<TypeAndPath> metadata = new List<TypeAndPath>();

        internal class TypeAndPath
        {
            OperationType type;
            String forPath;

            internal TypeAndPath(OperationType type, String forPath)
            {
                this.type = type;
                this.forPath = forPath;
            }
        }

        public void add(Op op)
        {
            throw new NotImplementedException();
        }

        internal void add(Op op, OperationType type, String forPath)
        {
            base.add(op);
            metadata.Add(new TypeAndPath(type, forPath));
        }

        internal TypeAndPath getMetadata(int index)
        {
            return metadata[index];
        }

        internal int metadataSize()
        {
            return metadata.Count;
        }
    }
}