namespace Org.Apache.CuratorNet.Framework.API
{
    public interface ACLVersionBackgroundPathable<T> :
        IACLable<Versionable<BackgroundPathable<T>>>,
        Versionable<BackgroundPathable<T>>
    {
    }

}
