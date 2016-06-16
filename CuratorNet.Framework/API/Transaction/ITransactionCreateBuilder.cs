namespace Org.Apache.CuratorNet.Framework.API.Transaction
{
    public interface ITransactionCreateBuilder :
        PathAndBytesable<ICuratorTransactionBridge>,
        ICreateModable<IACLPathAndBytesable<ICuratorTransactionBridge>>,
        IACLPathAndBytesable<ICuratorTransactionBridge>,
        IACLCreateModePathAndBytesable<ICuratorTransactionBridge>,
        Compressible<IACLCreateModePathAndBytesable<ICuratorTransactionBridge>>  {
    }
}
