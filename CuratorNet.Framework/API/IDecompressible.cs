namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IDecompressible<T>
    {
        /**
         * Cause the data to be de-compressed using the configured compression provider
         *
         * @return this
         */
        T decompressed();
    }

}
