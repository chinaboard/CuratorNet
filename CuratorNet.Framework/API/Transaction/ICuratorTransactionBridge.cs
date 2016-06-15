using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Apache.CuratorNet.Framework.API.Transaction
{
    public interface ICuratorTransactionBridge
    {
        /**
         * Syntactic sugar to make the fluent interface more readable
         *
         * @return transaction continuation
         */
        CuratorTransactionFinal and();
    }
}
