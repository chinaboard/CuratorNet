namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IBackgroundPathAndBytesable<T> :
        IBackgroundable<ErrorListenerPathAndBytesable<T>>,
        PathAndBytesable<T>
    {
    }
}
