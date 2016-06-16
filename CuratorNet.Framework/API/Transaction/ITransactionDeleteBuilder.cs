namespace Org.Apache.CuratorNet.Framework.API.Transaction
{
    public interface ITransactionDeleteBuilder<T> :
        IPathable<T>,
        IVersionable<IPathable<T>>
    {
    }
}
