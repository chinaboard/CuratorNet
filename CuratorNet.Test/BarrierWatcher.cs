using System.Threading;
using System.Threading.Tasks;
using org.apache.zookeeper;

namespace CuratorNet.Test
{
    public class BarrierWatcher : Watcher
    {
        protected readonly Barrier Barrier;

        public BarrierWatcher(Barrier barrier)
        {
            Barrier = barrier;
        }

        public override Task process(WatchedEvent @event)
        {
            Barrier.SignalAndWait(0);
            return Task.FromResult<object>(null);
        }
    }
}
