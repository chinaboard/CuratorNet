namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IACLCreateModePathAndBytesable<T> :
        IACLPathAndBytesable<T>,
        CreateModable<ACLPathAndBytesable<T>>
    {
    }
}
