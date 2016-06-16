namespace Org.Apache.CuratorNet.Framework.API.Transaction
{
    internal interface ITransactionCheckBuilder<T> : IPathable<T>,IVersionable<IPathable<T>>
    {
    }
}
