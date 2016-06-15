namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IACLCreateModeBackgroundPathAndBytesable<T> :
        IACLBackgroundPathAndBytesable<T>,
        BackgroundPathAndBytesable<T>,
        CreateModable<ACLBackgroundPathAndBytesable<T>>
    {
    }
}
