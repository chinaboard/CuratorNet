using System;

namespace Org.Apache.CuratorNet.Framework
{
    public class AuthInfo
    {
        readonly string    scheme;
        readonly byte[] auth;

        public AuthInfo(string scheme, byte[] auth)
        {
            this.scheme = scheme;
            this.auth = auth;
        }

        public string getScheme()
        {
            return scheme;
        }

        public byte[] getAuth()
        {
            return auth;
        }

        public string toString()
        {
            return "AuthInfo{" +
                "scheme='" + scheme + '\'' +
                ", auth=" + BitConverter.ToString(auth) +
                '}';
        }
    }
}