namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IGetDataBuilder :
        IWatchable<IBackgroundPathable<byte[]>>,
        IBackgroundPathable<byte[]>,
        IStatable<IWatchPathable<byte[]>>,
        IDecompressible<IGetDataWatchBackgroundStatable>
    {
    }
}
