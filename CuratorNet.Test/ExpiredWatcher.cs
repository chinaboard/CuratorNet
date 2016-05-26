using System.Threading;
using System.Threading.Tasks;
using org.apache.zookeeper;

namespace CuratorNet.Test
{
    public class ExpiredWatcher : BarrierWatcher
    {
        public ExpiredWatcher(Barrier barrier) : base(barrier) { }

        public override Task process(WatchedEvent @event)
        {
            if (@event.getState() == Event.KeeperState.Expired)
            {
                Barrier.SignalAndWait(0);
            }
            return Task.FromResult<object>(null);
        }
    }
}
