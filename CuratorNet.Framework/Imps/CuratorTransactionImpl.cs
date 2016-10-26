using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.CuratorNet.Framework.API.Transaction;
using Org.Apache.Java.Types.Concurrent;
using Org.Apache.Java.Types.Concurrent.Atomics;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class CuratorTransactionImpl : ICuratorTransaction, ICuratorTransactionBridge, CuratorTransactionFinal
    {
        private readonly CuratorFrameworkImpl              client;
        private readonly CuratorMultiTransactionRecord     transaction;

        private bool isCommitted = false;

        internal CuratorTransactionImpl(CuratorFrameworkImpl client)
        {
            this.client = client;
            transaction = new CuratorMultiTransactionRecord();
        }

        public CuratorTransactionFinal and()
        {
            return this;
        }

        private void CheckAlreadyCommited()
        {
            if (isCommitted)
            {
                throw new InvalidOperationException("transaction already committed");
            }
        }

        public ITransactionCreateBuilder create()
        {
            CheckAlreadyCommited();
            return new CreateBuilderImpl(client).asTransactionCreateBuilder(this, transaction);
        }

        public ITransactionDeleteBuilder delete()
        {
            CheckAlreadyCommited();
            return new DeleteBuilderImpl(client).asTransactionDeleteBuilder(this, transaction);
        }

        public ITransactionSetDataBuilder setData()
        {
            CheckAlreadyCommited();
            return new SetDataBuilderImpl(client).asTransactionSetDataBuilder(this, transaction);
        }

        internal class TransactionCheckBuilder : ITransactionCheckBuilder<ICuratorTransactionBridge>
        {
            private readonly CuratorTransactionImpl _curatorTransactionImpl;
            private int version = -1;

            public TransactionCheckBuilder(CuratorTransactionImpl curatorTransactionImpl)
            {
                _curatorTransactionImpl = curatorTransactionImpl;
            }

            public ICuratorTransactionBridge forPath(String path)
            {
                String fixedPath = _curatorTransactionImpl.client.fixForNamespace(path);
                Op op = Op.check(fixedPath, version);
                _curatorTransactionImpl.transaction.add(op, OperationType.CHECK, path);
                return _curatorTransactionImpl;
            }

            public IPathable<ICuratorTransactionBridge> withVersion(int version)
            {
                this.version = version;
                return this;
            }
        }

        public ITransactionCheckBuilder<ICuratorTransactionBridge> check()
        {
            CheckAlreadyCommited();
            return new TransactionCheckBuilder(this);
        }

        public ICollection<CuratorTransactionResult> commit()
        {
            CheckAlreadyCommited();
            isCommitted = true;

            AtomicBoolean firstTime = new AtomicBoolean(true);
            List<OpResult> resultList = RetryLoop.callWithRetry
            (
                client.getZookeeperClient(),
                CallableUtils.FromFunc(() => doOperation(firstTime)));
        
            if ( resultList.Count != transaction.metadataSize() )
            {
                throw new InvalidOperationException(String.Format("Result size ({0}) doesn't match input size ({1})", 
                                                        resultList.Count, 
                                                        transaction.metadataSize()));
            }

            var builder = new ReadOnlyCollectionBuilder<CuratorTransactionResult>();
            for ( int i = 0; i<resultList.Count; ++i )
            {
                OpResult opResult = resultList[i];
                CuratorMultiTransactionRecord.TypeAndPath metadata = transaction.getMetadata(i);
                CuratorTransactionResult curatorResult = makeCuratorResult(opResult, metadata);
                builder.Add(curatorResult);
            }

            return builder.ToReadOnlyCollection();
        }

        private List<OpResult> doOperation(AtomicBoolean firstTime) 
        {
            bool localFirstTime = firstTime.getAndSet(false);
            if ( !localFirstTime )
            {

            }

            List<OpResult>  opResults = client.getZooKeeper().multi(transaction);
            if ( opResults.Count > 0 )
            {
                OpResult firstResult = opResults[0];
                if (firstResult is OpResult.ErrorResult)
                {
                    OpResult.ErrorResult error = (OpResult.ErrorResult)firstResult;
                    KeeperException.Code code = KeeperException.Code.get(error.getErr());
                    if (code == null)
                    {
                        code = KeeperException.Code.UNIMPLEMENTED;
                    }
                    throw KeeperException.create(code);
                }
            }
            return opResults;
        }

        private CuratorTransactionResult makeCuratorResult(OpResult opResult, 
                                                            CuratorMultiTransactionRecord.TypeAndPath metadata)
        {
            String resultPath = null;
            Stat resultStat = null;
            if (opResult is OpResult.CreateResult)
            {
                OpResult.CreateResult createResult = (OpResult.CreateResult)opResult;
                resultPath = client.unfixForNamespace(createResult.getPath());
            }
            else if (opResult is OpResult.SetDataResult)
            {
                OpResult.SetDataResult setDataResult = (OpResult.SetDataResult) opResult;
                resultStat = setDataResult.getStat();
            }
            return new CuratorTransactionResult(metadata.type, metadata.forPath, resultPath, resultStat);
        }
    }
}