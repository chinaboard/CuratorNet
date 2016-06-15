using System.Collections.Generic;

namespace Org.Apache.CuratorNet.Framework.API.Transaction
{
    /**
     * Adds commit to the transaction interface
     */
    public interface CuratorTransactionFinal : ICuratorTransaction
    {
        /**
         * Commit all added operations as an atomic unit and return results
         * for the operations. One result is returned for each operation added.
         * Further, the ordering of the results matches the ordering that the
         * operations were added.
         *
         * @return operation results
         * @throws Exception errors
         */
        ICollection<ICuratorTransactionResult> commit();
    }
}
