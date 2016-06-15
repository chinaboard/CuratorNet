namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IBackgroundPathable<T> :
        IBackgroundable<ErrorListenerPathable<T>>,
        IPathable
    {
    }
}
