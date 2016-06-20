namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class PathAndBytes
    {
        private readonly string path;
        private readonly byte[] data;

        PathAndBytes(string path, byte[] data)
        {
            this.path = path;
            this.data = data;
        }

        string getPath()
        {
            return path;
        }

        byte[] getData()
        {
            return data;
        }
    }
}