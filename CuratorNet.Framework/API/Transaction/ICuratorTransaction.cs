using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Apache.CuratorNet.Framework.API.Transaction
{
    /**
     * <p>
     *     Transactional/atomic operations. See {@link ZooKeeper#multi(Iterable)} for
     *     details on ZooKeeper transactions.
     * </p>
     *
     * <p>
     *     The general form for this interface is:
     * </p>
     *     <pre>
     *         curator.inTransaction().operation().arguments().forPath(...).
     *             and().more-operations.
     *             and().commit();
     *     </pre>
     *
     * <p>
     *     Here's an example that creates two nodes in a transaction
     * </p>
     *     <pre>
     *         curator.inTransaction().
     *             create().forPath("/path-one", path-one-data).
     *             and().create().forPath("/path-two", path-two-data).
     *             and().commit();
     *     </pre>
     *
     * <p>
     *     <b>Important:</b> the operations are not submitted until
     *     {@link CuratorTransactionFinal#commit()} is called.
     * </p>
    */
    public interface ICuratorTransaction
    {
        /**
         * Start a create builder in the transaction
         *
         * @return builder object
         */
        TransactionCreateBuilder create();

        /**
         * Start a delete builder in the transaction
         *
         * @return builder object
         */
        TransactionDeleteBuilder delete();

        /**
         * Start a setData builder in the transaction
         *
         * @return builder object
         */
        TransactionSetDataBuilder setData();

        /**
         * Start a check builder in the transaction
         *ChildData
         * @return builder object
         */
        TransactionCheckBuilder check();
    }
}
