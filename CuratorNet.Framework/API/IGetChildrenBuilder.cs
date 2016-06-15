using System;
using System.Collections.Generic;

namespace Org.Apache.CuratorNet.Framework.API
{
    public interface IGetChildrenBuilder :
        IWatchable<IBackgroundPathable<List<String>>>,
        IBackgroundPathable<List<String>>,
        IStatable<IWatchPathable<List<String>>>
    {
    }
}
