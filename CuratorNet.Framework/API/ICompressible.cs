namespace Org.Apache.CuratorNet.Framework.API
{
    public interface Compressible<T>
    {
        /**
         * Cause the data to be compressed using the configured compression provider
         *
         * @return this
         */
        T compressed();
    }
}
