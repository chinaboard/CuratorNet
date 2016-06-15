namespace Org.Apache.CuratorNet.Framework.API
{
    public interface ICreateModalPathAndBytesable<T> :
        ICreateModable<IPathAndBytesable<T>>,
        IPathAndBytesable<T>
    {
    }

}
