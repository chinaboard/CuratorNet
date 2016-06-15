namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IWatchPathable<T> :
        IWatchable<IPathable<T>>,
        IPathable<T>
    {
    }
}