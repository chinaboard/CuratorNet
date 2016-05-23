using System;

namespace Org.Apache.CuratorNet.Client.Utils
{
    public class PathUtils
    {
        /** validate the provided znode path string
         * @param path znode path string
         * @param isSequential if the path is being created
         * with a sequential flag
         * @throws IllegalArgumentException if the path is invalid
         */
        public static void validatePath(String path, bool isSequential)
        {
            validatePath(isSequential? path + "1": path);
        }

        /**
         * Validate the provided znode path string
         * @param path znode path string
         * @return The given path if it was valid, for fluent chaining
         * @throws IllegalArgumentException if the path is invalid
         */
        public static String validatePath(String path)
        {
            if (path == null)
            {
                throw new ArgumentException("Path cannot be null");
            }
            if (path.Length == 0)
            {
                throw new ArgumentException("Path length must be > 0");
            }
            if (path[0] != '/')
            {
                throw new ArgumentException("Path must start with / character");
            }
            if (path.Length == 1)
            {
                return path;
            }
            if (path[path.Length - 1] == '/')
            {
                throw new ArgumentException("Path must not end with / character");
            }

            String reason = null;
            char lastc = '/';
            char[] chars = path.ToCharArray();
            char c;
            for (int i = 1; i < chars.Length; lastc = chars[i], i++)
            {
                c = chars[i];

                if (c == 0)
                {
                    reason = "null character not allowed @" + i;
                    break;
                }
                else if (c == '/' && lastc == '/')
                {
                    reason = "empty node name specified @" + i;
                    break;
                }
                else if (c == '.' && lastc == '.')
                {
                    if (chars[i - 2] == '/' &&
                            ((i + 1 == chars.Length)
                                    || chars[i + 1] == '/'))
                    {
                        reason = "relative paths not allowed @" + i;
                        break;
                    }
                }
                else if (c == '.')
                {
                    if (chars[i - 1] == '/' &&
                            ((i + 1 == chars.Length)
                                    || chars[i + 1] == '/'))
                    {
                        reason = "relative paths not allowed @" + i;
                        break;
                    }
                }
                else if (c > '\u0000' && c< '\u001f'
                        || c> '\u007f' && c< '\u009F'
                        || c> '\ud800' && c< '\uf8ff'
                        || c> '\ufff0' && c< '\uffff')
                {
                    reason = "invalid charater @" + i;
                    break;
                }
            }

            if (reason != null)
            {
                throw new ArgumentException("Invalid path string \"" + 
                                            path + "\" caused by " + reason);
            }

            return path;
        }
    }
}
