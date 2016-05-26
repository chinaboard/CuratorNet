using System;
using System.Threading;
using System.Threading.Tasks;
using org.apache.zookeeper;

namespace CuratorNet.Test
{
    /**
     * <p>
     *     Utility to simulate a ZK session dying. See: <a href="http://wiki.apache.org/hadoop/ZooKeeper/FAQ#A4">ZooKeeper FAQ</a>
     * </p>
     *
     * <blockquote>
     *     In the case of testing we want to cause a problem, so to explicitly expire a session an
     *     application connects to ZooKeeper, saves the session id and password, creates another
     *     ZooKeeper handle with that id and password, and then closes the new handle. Since both
     *     handles reference the same session, the close on second handle will invalidate the session
     *     causing a SESSION_EXPIRED on the first handle.
     * </blockquote>
     */
    public class KillSession
    {
        /**
         * Kill the given ZK session
         *
         * @param client the client to kill
         * @param connectString server connection string
         * @throws Exception errors
         */
//        public static void kill(ZooKeeper client, String connectString)
//        {
//            kill(client, connectString, new Timing().forWaiting().milliseconds());
//        }

        class SyncWatcher : BarrierWatcher
        {
            public SyncWatcher(Barrier barrier) : base(barrier) {}

            public override Task process(WatchedEvent @event)
            {
                if ( @event.getState() == Watcher.Event.KeeperState.SyncConnected )
                {
                    Barrier.SignalAndWait(0);
                }
                return Task.FromResult<object>(null);
            }
        }

        /**
         * Kill the given ZK session
         *
         * @param client the client to kill
         * @param connectString server connection string
         * @param maxMs max time ms to wait for kill
         * @throws Exception errors
         */
        public static void kill(ZooKeeper client, String connectString, int maxMs)
        {
            long startTicks = DateTime.Now.Ticks / 1000;

            Barrier sessionLostLatch = new Barrier(2);
            Watcher sessionLostWatch = new BarrierWatcher(sessionLostLatch);
            client.existsAsync("/___CURATOR_KILL_SESSION___" + DateTime.Now.Ticks, 
                                     sessionLostWatch)
                  .Wait();

            Barrier connectionLatch = new Barrier(2);
            Watcher connectionWatcher = new SyncWatcher(connectionLatch);
            ZooKeeper zk = new ZooKeeper(connectString, 
                                            maxMs, 
                                            connectionWatcher, 
                                            client.getSessionId(), 
                                            client.getSessionPasswd());
            try
            {
                if ( !connectionLatch.SignalAndWait(maxMs) )
                {
                    throw new Exception("KillSession could not establish duplicate session");
                }
                try
                {
                    zk.closeAsync().Wait();
                }
                finally
                {
                    zk = null;
                }

                while ( client.getState() == ZooKeeper.States.CONNECTED 
                            && !sessionLostLatch.SignalAndWait(100) )
                {
                    long elapsed = (DateTime.Now.Ticks / 1000) - startTicks;
                    if ( elapsed > maxMs )
                    {
                        throw new Exception("KillSession timed out waiting for session to expire");
                    }
                }
            }
            finally
            {
                zk?.closeAsync().Wait();
            }
        }
    }
}
