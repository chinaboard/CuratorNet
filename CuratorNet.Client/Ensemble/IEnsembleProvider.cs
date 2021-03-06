﻿using System;

namespace Org.Apache.CuratorNet.Client.Ensemble
{
    /**
     * Abstraction that provides the ZooKeeper connection string
     */
    public interface IEnsembleProvider : IDisposable
    {
        /**
         * Curator will call this method when {@link CuratorZookeeperClient#start()} is
         * called
         * 
         * @throws Exception errors
         */
        void start();

        /**
         * Return the current connection string to use. Curator will call this each
         * time it needs to create a ZooKeeper instance
         * 
         * @return connection string (per {@link ZooKeeper#ZooKeeper(String, int, Watcher)} etc.)
         */
        String getConnectionString();
    }
}
