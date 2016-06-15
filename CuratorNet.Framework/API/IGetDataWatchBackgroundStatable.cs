namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IGetDataWatchBackgroundStatable :
        IWatchable<IBackgroundPathable<byte[]>>,
        IBackgroundPathable<byte[]>,
        IStatable<IWatchPathable<byte[]>>
    {
    }
}
