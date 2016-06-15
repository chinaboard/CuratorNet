using System;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IBackgroundVersionable :
        IBackgroundPathable<Void>,
        Versionable<BackgroundPathable<Void>>
    {
    }
}
