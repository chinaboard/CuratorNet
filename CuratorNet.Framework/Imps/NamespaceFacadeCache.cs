using System.Runtime.Caching;

namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class NamespaceFacadeCache
    {
        private readonly CuratorFrameworkImpl                  client;
        private readonly NamespaceFacade                       nullNamespace;
        private readonly MemoryCache _cache = new MemoryCache(nameof(NamespaceFacadeCache));
        
        internal NamespaceFacadeCache(CuratorFrameworkImpl client)
        {
            this.client = client;
            nullNamespace = new NamespaceFacade(client, null);
        }

        internal NamespaceFacade get(string @namespace)
        {
            if (@namespace == null)
            {
                return nullNamespace;
            }
            var namespaceFacade = (NamespaceFacade)_cache[@namespace];
            if (namespaceFacade == null)
            {
                namespaceFacade = new NamespaceFacade(client, @namespace);
                _cache[@namespace] = namespaceFacade;
            }
            return namespaceFacade;
        }
    }
}