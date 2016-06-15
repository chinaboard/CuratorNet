namespace Org.Apache.CuratorNet.Framework.API.Transaction
{
    internal interface ITransactionCheckBuilder :
        IPathable,
        Versionable<IPathable>
    {
    }
}
