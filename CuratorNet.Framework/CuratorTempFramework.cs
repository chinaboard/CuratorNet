using System;
using Org.Apache.CuratorNet.Framework.API;
using Org.Apache.CuratorNet.Framework.API.Transaction;

namespace Org.Apache.CuratorNet.Framework
{
    /**
     * <p>
     *     Temporary CuratorFramework instances are meant for single requests to ZooKeeper ensembles
     *     over a failure prone network such as a WAN. The APIs available from CuratorTempFramework
     *     are limited. Further, the connection will be closed after a period of inactivity.
     * </p>
     *
     * <p>
     *     Based on an idea mentioned in a post by Camille Fournier:
     *     <a href="http://whilefalse.blogspot.com/2012/12/building-global-highly-available.html">http://whilefalse.blogspot.com/2012/12/building-global-highly-available.html</a>
     * </p>
     */
    public interface CuratorTempFramework : IDisposable
    {
        /**
         * Stop the client
         */
        void Dispose();

        /**
         * Start a transaction builder
         *
         * @return builder object
         * @throws Exception errors
         */
        ICuratorTransaction inTransaction();

        /**
         * Start a get data builder
         *
         * @return builder object
         * @throws Exception errors
         */
        ITempGetDataBuilder getData();
    }
}