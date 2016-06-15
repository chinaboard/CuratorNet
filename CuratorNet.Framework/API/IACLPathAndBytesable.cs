namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IACLPathAndBytesable<T> :
        IACLable<PathAndBytesable<T>>,
        PathAndBytesable<T>
    {
    }
}
