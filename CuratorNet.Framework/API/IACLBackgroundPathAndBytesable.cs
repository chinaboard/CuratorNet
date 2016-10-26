namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IACLBackgroundPathAndBytesable<T> :
        IACLable<IBackgroundPathAndBytesable<T>>,
        IBackgroundPathAndBytesable<T>
    {
    }
}
