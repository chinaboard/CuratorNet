namespace Org.Apache.CuratorNet.Framework.API
{
    public interface ITempGetDataBuilder :
        IStatPathable<byte[]>,
        IDecompressible<IStatPathable<byte[]>>,
        IPathable<byte[]>
    {
    }
}