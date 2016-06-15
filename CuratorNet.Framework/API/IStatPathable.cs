namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IStatPathable<T> :
        IPathable<T>,
        IStatable<IPathable<T>>
    {
    }
}
