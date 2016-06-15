namespace Org.Apache.CuratorNet.Framework.API.Transaction
{
    public interface ITransactionCreateBuilder :
        PathAndBytesable<CuratorTransactionBridge>,
        CreateModable<ACLPathAndBytesable<CuratorTransactionBridge>>,
        ACLPathAndBytesable<CuratorTransactionBridge>,
        ACLCreateModePathAndBytesable<CuratorTransactionBridge>,
        Compressible<ACLCreateModePathAndBytesable<CuratorTransactionBridge>>  {
    }
}
