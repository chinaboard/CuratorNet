using System;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface ICompressionProvider
    {
        byte[] compress(String path, byte[] data);

        byte[] decompress(String path, byte[] compressedData);
    }
}
