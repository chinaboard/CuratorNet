namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal interface IBackgroundOperation<T>
    {
        void performBackgroundOperation(OperationAndData<T> data);
    }
}
