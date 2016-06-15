using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Apache.CuratorNet.Framework.API.Transaction
{
    public interface ITransactionSetDataBuilder :
        PathAndBytesable<CuratorTransactionBridge>,
        Versionable<PathAndBytesable<CuratorTransactionBridge>>,
        VersionPathAndBytesable<CuratorTransactionBridge>,
        Compressible<VersionPathAndBytesable<CuratorTransactionBridge>>
    {
    }
}
