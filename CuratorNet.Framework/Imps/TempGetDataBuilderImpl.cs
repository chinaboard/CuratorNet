using System;
using org.apache.zookeeper.data;
using Org.Apache.CuratorNet.Client;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.Java.Types.Concurrent;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class TempGetDataBuilderImpl : ITempGetDataBuilder
    {
        private readonly CuratorFrameworkImpl  client;
        private Stat responseStat;
        private bool decompress;

        TempGetDataBuilderImpl(CuratorFrameworkImpl client)
        {
            this.client = client;
            responseStat = null;
            decompress = false;
        }

        public IStatPathable<byte[]> decompressed()
        {
            decompress = true;
            return this;
        }

        public IPathable<byte[]> storingStatIn(Stat stat)
        {
            responseStat = stat;
            return this;
        }

        public byte[] forPath(String path) 
        {
            String localPath = client.fixForNamespace(path);

            TimeTrace trace = client.getZookeeperClient().startTracer("GetDataBuilderImpl-Foreground");
            byte[]
            responseData = RetryLoop.callWithRetry
            (
                client.getZookeeperClient(),
                CallableUtils.FromFunc(() =>
                {
                    var dataAsync = client.getZooKeeper().getDataAsync(localPath, false);
                    dataAsync.Wait();
                    responseStat = dataAsync.Result.Stat;
                    return dataAsync.Result.Data;
                })
            );
            trace.commit();

            return decompress 
                    ? client.getCompressionProvider().decompress(path, responseData) 
                    : responseData;
        }
    }
}