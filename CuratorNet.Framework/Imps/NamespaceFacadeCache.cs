namespace Org.Apache.CuratorNet.Framework.Imps
{
    internal class NamespaceFacadeCache
    {
        private readonly CuratorFrameworkImpl                  client;
        private readonly NamespaceFacade                       nullNamespace;
        private readonly CacheLoader<string, NamespaceFacade>  loader = new CacheLoader<string, NamespaceFacade>()
        {
            public NamespaceFacade load(string @namespace)
            {
                return new NamespaceFacade(client, @namespace);
            }
        };

        private readonly LoadingCache<String, NamespaceFacade> cache 
                = CacheBuilder.newBuilder()
                                .expireAfterAccess(5, TimeUnit.MINUTES) // does this need config? probably not
                                .build(loader);

        internal NamespaceFacadeCache(CuratorFrameworkImpl client)
        {
            this.client = client;
            nullNamespace = new NamespaceFacade(client, null);
        }

        internal NamespaceFacade get(string @namespace)
        {
            try
            {
                return (@namespace != null) ? cache.get(@namespace) : nullNamespace;
            }
            catch ( ExecutionException e )
            {
                throw new RuntimeException(e);  // should never happen
            }
        }
    }
}