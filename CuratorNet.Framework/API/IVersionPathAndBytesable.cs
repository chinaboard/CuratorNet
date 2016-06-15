namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IVersionPathAndBytesable<T> :
            IVersionable<PathAndBytesable<T>>,
            PathAndBytesable<T>
    {
    }
}
