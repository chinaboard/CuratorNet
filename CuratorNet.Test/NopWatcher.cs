using System.Threading.Tasks;
using org.apache.zookeeper;

namespace CuratorNet.Test
{
    public class NopWatcher : Watcher
    {
        public override Task process(WatchedEvent @event)
        {
            return Task.FromResult<object>(null);
        }
    }
}
