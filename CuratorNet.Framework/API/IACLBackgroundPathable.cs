namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IACLBackgroundPathable<T> :
        IACLable<BackgroundPathable<T>>,
        BackgroundPathable<T>
    {
    }
}
