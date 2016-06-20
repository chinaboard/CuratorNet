using System;
using System.IO;
using System.IO.Compression;
using Org.Apache.CuratorNet.Framework.API;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    public class GzipCompressionProvider : ICompressionProvider
    {
        public byte[] compress(String path, byte[] data)
        {
            var bytes = new MemoryStream();
            var outGziped = new GZipStream(bytes, CompressionLevel.Fastest);
            outGziped.Write(data, 0, data.Length);
            outGziped.Flush();
            return bytes.ToArray();
        }

        public byte[] decompress(String path, byte[] compressedData)
        {
            var bytes = new MemoryStream(compressedData.Length);
            var inGzip = new GZipStream(new MemoryStream(compressedData),CompressionLevel.Fastest);
            byte[] buffer = new byte[compressedData.Length];
            for(;;)
            {
                int bytesRead = inGzip.Read(buffer, 0, buffer.Length);
                if ( bytesRead< 0 )
                {
                    break;
                }
                bytes.Write(buffer, 0, bytesRead);
            }
            return bytes.ToArray();
        }
    }
}