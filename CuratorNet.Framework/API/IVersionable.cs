namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IVersionable<T>
    {
        /**
         * Use the given version (the default is -1)
         *
         * @param version version to use
         * @return this
         */
        T withVersion(int version);
    }
}