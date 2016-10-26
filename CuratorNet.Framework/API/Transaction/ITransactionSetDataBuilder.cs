namespace Org.Apache.CuratorNet.Framework.API.Transaction
{
    public interface ITransactionSetDataBuilder :
        PathAndBytesable<ICuratorTransactionBridge>,
        IVersionable<PathAndBytesable<ICuratorTransactionBridge>>,
        IVersionPathAndBytesable<ICuratorTransactionBridge>,
        Compressible<IVersionPathAndBytesable<ICuratorTransactionBridge>>
    {
    }
}
